#!/bin/sh
set -eu
umask 077

EX_USAGE=64
EX_LOCK=65
EX_POSTGRES=66
EX_S3=67
EX_VERIFY=68
EX_RESTORE=69
EX_RETENTION=70

WORK_ROOT=${BACKUP_WORK_ROOT:-/backup-work}
operation=${1:-}

case "$operation" in
    backup|list|resolve|describe|restore)
        shift
        ;;
    "")
        echo "usage: db-backup <backup|list|resolve|describe|restore>" >&2
        exit "$EX_USAGE"
        ;;
    *)
        # image smoke testsと運用時の診断コマンドを、同じclient imageで実行できるようにします。
        exec "$operation" "$@"
        ;;
esac

die() {
    code=$1
    shift
    echo "db-backup: $*" >&2
    exit "$code"
}

require_value() {
    variable_name=$1
    variable_value=$2
    [ -n "$variable_value" ] ||
        die "$EX_USAGE" "$variable_name is required"
}

require_value PGHOST "${PGHOST:-}"
require_value PGPORT "${PGPORT:-}"
require_value PGDATABASE "${PGDATABASE:-}"
require_value PGUSER "${PGUSER:-}"
require_value PGPASSWORD "${PGPASSWORD:-}"
require_value S3_BUCKET "${S3_BUCKET:-}"
require_value AWS_REGION "${AWS_REGION:-}"
require_value AWS_ACCESS_KEY_ID "${AWS_ACCESS_KEY_ID:-}"
require_value AWS_SECRET_ACCESS_KEY "${AWS_SECRET_ACCESS_KEY:-}"

retention=${BACKUP_RETENTION_COUNT:-4}
case "$retention" in
    ''|*[!0-9]*|0)
        die "$EX_USAGE" "BACKUP_RETENTION_COUNT must be an integer greater than zero"
        ;;
esac

addressing_style=${S3_ADDRESSING_STYLE:-auto}
case "$addressing_style" in
    auto|path|virtual) ;;
    *) die "$EX_USAGE" "S3_ADDRESSING_STYLE must be auto, path, or virtual" ;;
esac

endpoint_url=${S3_ENDPOINT_URL:-}
case "$endpoint_url" in
    http://*)
        [ "${ALLOW_INSECURE_S3_ENDPOINT:-false}" = "true" ] ||
            die "$EX_USAGE" "HTTP S3 endpoint requires ALLOW_INSECURE_S3_ENDPOINT=true"
        ;;
    https://*|'') ;;
    *) die "$EX_USAGE" "S3_ENDPOINT_URL must use HTTPS" ;;
esac

prefix=${S3_PREFIX:-backups}
while [ "${prefix#"/"}" != "$prefix" ]; do prefix=${prefix#"/"}; done
while [ -n "$prefix" ] && [ "${prefix%"/"}" != "$prefix" ]; do prefix=${prefix%"/"}; done
[ -n "$prefix" ] || die "$EX_USAGE" "S3_PREFIX must not be empty"

mkdir -p "$WORK_ROOT"
exec 9>"$WORK_ROOT/operation.lock"
flock -n 9 || die "$EX_LOCK" "another backup operation is running"

temporary_directory=$(mktemp -d "$WORK_ROOT/db-backup.XXXXXX")
cleanup() {
    rm -rf "$temporary_directory"
}
trap cleanup EXIT INT TERM

AWS_CONFIG_FILE="$temporary_directory/aws-config"
export AWS_CONFIG_FILE
export AWS_EC2_METADATA_DISABLED=true
cat >"$AWS_CONFIG_FILE" <<EOF
[default]
region = $AWS_REGION
s3 =
    addressing_style = $addressing_style
EOF

bucket_uri="s3://$S3_BUCKET"
root_uri="$bucket_uri/$prefix"

aws_s3() {
    if [ -n "$endpoint_url" ]; then
        aws --endpoint-url "$endpoint_url" s3 "$@"
    else
        aws s3 "$@"
    fi
}

download_object() {
    key=$1
    destination=$2
    aws_s3 cp "$bucket_uri/$key" "$destination" --only-show-errors ||
        return "$EX_S3"
}

manifest_is_valid() {
    manifest_file=$1
    manifest_key=$2
    expected_dump_key=${manifest_key%.json}.dump

    jq -e \
        --arg database "$PGDATABASE" \
        --arg dump_key "$expected_dump_key" \
        '
        .database == $database
        and .dumpKey == $dump_key
        and .format == "pg_dump-custom"
        and (.createdAtUtc | type == "string" and test("^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z$"))
        and (.sizeBytes | type == "number" and . >= 0 and floor == .)
        and (.sha256 | type == "string" and test("^[0-9a-f]{64}$"))
        ' \
        "$manifest_file" >/dev/null 2>&1
}

manifest_keys() {
    listing_file="$temporary_directory/s3-listing"
    aws_s3 ls "$root_uri/" --recursive >"$listing_file" ||
        return "$EX_S3"
    awk '$4 ~ /\.json$/ { print $4 }' "$listing_file"
}

manifest_records() {
    index=0
    keys_file="$temporary_directory/manifest-keys"
    unsorted_file="$temporary_directory/manifest-records-unsorted"
    manifest_keys >"$keys_file" || return "$?"
    : >"$unsorted_file"

    while IFS= read -r manifest_key; do
        [ -n "$manifest_key" ] || continue
        index=$((index + 1))
        manifest_file="$temporary_directory/manifest-$index.json"
        if ! download_object "$manifest_key" "$manifest_file"; then
            return "$EX_S3"
        fi
        if manifest_is_valid "$manifest_file" "$manifest_key"; then
            jq -r '[.createdAtUtc, (.sizeBytes | tostring), .sha256, .dumpKey] | @tsv' \
                "$manifest_file" >>"$unsorted_file"
        fi
    done <"$keys_file"
    sort -r "$unsorted_file"
}

describe_key() {
    dump_key=$1
    case "$dump_key" in
        "$prefix"/vrcwebmap-*.dump) ;;
        *) die "$EX_USAGE" "dump key must match $prefix/vrcwebmap-*.dump" ;;
    esac

    manifest_key=${dump_key%.dump}.json
    manifest_file="$temporary_directory/describe.json"
    download_object "$manifest_key" "$manifest_file" ||
        die "$EX_S3" "failed to download manifest: $manifest_key"
    manifest_is_valid "$manifest_file" "$manifest_key" ||
        die "$EX_VERIFY" "manifest is invalid: $manifest_key"
    printf '%s\n' "$manifest_file"
}

perform_retention() {
    records_file="$temporary_directory/retention-records"
    manifest_records >"$records_file" ||
        die "$EX_RETENTION" "new backup succeeded, but complete generations could not be listed"

    awk -F '\t' -v keep="$retention" 'NR > keep { print $4 }' "$records_file" |
    while IFS= read -r old_dump_key; do
        [ -n "$old_dump_key" ] || continue
        old_base=${old_dump_key%.dump}
        for old_key in "$old_dump_key" "$old_base.sha256" "$old_base.json"; do
            aws_s3 rm "$bucket_uri/$old_key" --only-show-errors ||
                exit "$EX_RETENTION"
        done
    done || die "$EX_RETENTION" "new backup succeeded, but retention cleanup failed"
}

backup_database() {
    pg_isready --quiet ||
        die "$EX_POSTGRES" "PostgreSQL is not ready"
    aws_s3 ls "$bucket_uri" >/dev/null ||
        die "$EX_S3" "S3 bucket is not accessible"

    created_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)
    timestamp=$(date -u +%Y%m%dT%H%M%SZ)
    base_name="vrcwebmap-$timestamp"
    dump_key="$prefix/$base_name.dump"
    checksum_key="$prefix/$base_name.sha256"
    manifest_key="$prefix/$base_name.json"
    dump_file="$temporary_directory/$base_name.dump"
    checksum_file="$temporary_directory/$base_name.sha256"
    manifest_file="$temporary_directory/$base_name.json"

    pg_dump \
        --format=custom \
        --no-owner \
        --no-acl \
        --file "$dump_file" ||
        die "$EX_POSTGRES" "pg_dump failed"
    pg_restore --list "$dump_file" >/dev/null ||
        die "$EX_VERIFY" "new dump archive could not be read"

    size_bytes=$(wc -c <"$dump_file" | tr -d ' ')
    sha256=$(sha256sum "$dump_file" | awk '{ print $1 }')
    printf '%s  %s\n' "$sha256" "$base_name.dump" >"$checksum_file"

    jq -n \
        --arg database "$PGDATABASE" \
        --arg created_at "$created_at" \
        --arg dump_key "$dump_key" \
        --arg sha256 "$sha256" \
        --argjson size_bytes "$size_bytes" \
        '{
            database: $database,
            createdAtUtc: $created_at,
            dumpKey: $dump_key,
            sizeBytes: $size_bytes,
            sha256: $sha256,
            format: "pg_dump-custom"
        }' >"$manifest_file"

    # manifestを最後に置くことで、それ以前のupload失敗を完全世代として扱わせません。
    aws_s3 cp "$dump_file" "$bucket_uri/$dump_key" --only-show-errors ||
        die "$EX_S3" "dump upload failed"
    aws_s3 cp "$checksum_file" "$bucket_uri/$checksum_key" --only-show-errors ||
        die "$EX_S3" "checksum upload failed"
    aws_s3 cp "$manifest_file" "$bucket_uri/$manifest_key" --only-show-errors ||
        die "$EX_S3" "manifest upload failed"

    perform_retention
    echo "$dump_key"
}

list_backups() {
    records_file="$temporary_directory/list-records"
    manifest_records >"$records_file" ||
        die "$EX_S3" "failed to list backup manifests"
    cat "$records_file"
}

resolve_backup() {
    selector=${1:-}
    [ -n "$selector" ] || die "$EX_USAGE" "resolve requires latest or an object key"

    if [ "$selector" = "latest" ]; then
        records_file="$temporary_directory/resolve-records"
        manifest_records >"$records_file" ||
            die "$EX_S3" "failed to list backup manifests"
        resolved=$(awk -F '\t' 'NR == 1 { print $4 }' "$records_file")
        [ -n "$resolved" ] || die "$EX_VERIFY" "no complete backup generation exists"
        echo "$resolved"
        return
    fi

    manifest_file=$(describe_key "$selector")
    jq -r '.dumpKey' "$manifest_file"
}

describe_backup() {
    selector=${1:-}
    [ -n "$selector" ] || die "$EX_USAGE" "describe requires an object key"
    manifest_file=$(describe_key "$selector")
    jq -r '[.database, .createdAtUtc, (.sizeBytes | tostring), .sha256, .dumpKey] | @tsv' \
        "$manifest_file"
}

restore_database() {
    dump_key=${1:-}
    [ -n "$dump_key" ] || die "$EX_USAGE" "restore requires an exact dump object key"

    manifest_key=${dump_key%.dump}.json
    checksum_key=${dump_key%.dump}.sha256
    base_name=${dump_key##*/}
    dump_file="$temporary_directory/$base_name"
    checksum_file="$temporary_directory/${base_name%.dump}.sha256"
    manifest_file="$temporary_directory/${base_name%.dump}.json"

    download_object "$dump_key" "$dump_file" ||
        die "$EX_S3" "failed to download dump: $dump_key"
    download_object "$checksum_key" "$checksum_file" ||
        die "$EX_S3" "failed to download checksum: $checksum_key"
    download_object "$manifest_key" "$manifest_file" ||
        die "$EX_S3" "failed to download manifest: $manifest_key"

    manifest_is_valid "$manifest_file" "$manifest_key" ||
        die "$EX_VERIFY" "manifest is invalid: $manifest_key"

    manifest_size=$(jq -r '.sizeBytes' "$manifest_file")
    actual_size=$(wc -c <"$dump_file" | tr -d ' ')
    [ "$actual_size" = "$manifest_size" ] ||
        die "$EX_VERIFY" "dump size does not match manifest"

    manifest_hash=$(jq -r '.sha256' "$manifest_file")
    checksum_line_count=$(wc -l <"$checksum_file" | tr -d ' ')
    checksum_hash=$(awk 'NR == 1 { print $1 }' "$checksum_file")
    checksum_name=$(awk 'NR == 1 { print $2 }' "$checksum_file")
    [ "$checksum_line_count" = "1" ] &&
        [ "$checksum_hash" = "$manifest_hash" ] &&
        [ "$checksum_name" = "$base_name" ] ||
        die "$EX_VERIFY" "checksum object does not match manifest"

    actual_hash=$(sha256sum "$dump_file" | awk '{ print $1 }')
    [ "$actual_hash" = "$manifest_hash" ] ||
        die "$EX_VERIFY" "dump checksum verification failed"
    pg_restore --list "$dump_file" >/dev/null ||
        die "$EX_VERIFY" "dump archive could not be read"

    psql \
        --set ON_ERROR_STOP=1 \
        --command "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid();" \
        >/dev/null ||
        die "$EX_RESTORE" "failed to terminate database sessions"

    pg_restore \
        --clean \
        --if-exists \
        --exit-on-error \
        --single-transaction \
        --no-owner \
        --no-acl \
        --dbname "$PGDATABASE" \
        "$dump_file" ||
        die "$EX_RESTORE" "pg_restore failed; database was left unchanged"

    psql \
        --set ON_ERROR_STOP=1 \
        --command "ANALYZE" \
        >/dev/null ||
        die "$EX_RESTORE" "restore succeeded, but ANALYZE failed"

    echo "$dump_key"
}

case "$operation" in
    backup)
        [ "$#" -eq 0 ] || die "$EX_USAGE" "backup takes no arguments"
        backup_database
        ;;
    list)
        [ "$#" -eq 0 ] || die "$EX_USAGE" "list takes no arguments"
        list_backups
        ;;
    resolve)
        [ "$#" -eq 1 ] || die "$EX_USAGE" "resolve requires exactly one argument"
        resolve_backup "$1"
        ;;
    describe)
        [ "$#" -eq 1 ] || die "$EX_USAGE" "describe requires exactly one argument"
        describe_backup "$1"
        ;;
    restore)
        [ "$#" -eq 1 ] || die "$EX_USAGE" "restore requires exactly one dump object key"
        restore_database "$1"
        ;;
esac

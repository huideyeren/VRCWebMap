#!/bin/sh
set -eu

script_dir=$(CDPATH= cd -- "$(dirname "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_dir/../../.." && pwd)
compose_file="$script_dir/minio-compose.yml"
project_name="vrcwebmap-backup-integration"

dc() {
    docker compose \
        --project-name "$project_name" \
        --file "$compose_file" \
        "$@"
}

cleanup() {
    dc down --volumes --remove-orphans >/dev/null 2>&1 || true
}
trap cleanup EXIT INT TERM

fail() {
    echo "integration: $*" >&2
    exit 1
}

count_objects() {
    pattern=$1
    dc run --rm --no-deps minio-client \
        find "local/vrcwebmap-backups/backups" --name "$pattern" 2>/dev/null \
        | grep -c . || true
}

cd "$repository_root"
docker build -f ops/db-backup/Dockerfile -t vrcwebmap-db-backup:test . >/dev/null

cleanup
dc up --detach postgres minio

attempt=0
until dc exec --no-TTY postgres pg_isready -U vrcwebmap -d vrcwebmap >/dev/null 2>&1; do
    attempt=$((attempt + 1))
    [ "$attempt" -lt 30 ] || fail "PostgreSQL did not become ready"
    sleep 1
done

attempt=0
until dc run --rm --no-deps minio-client \
    alias set local http://minio:9000 minioadmin minioadmin >/dev/null 2>&1; do
    attempt=$((attempt + 1))
    [ "$attempt" -lt 30 ] || fail "MinIO did not become ready"
    sleep 1
done
dc run --rm --no-deps minio-client \
    mb --ignore-existing local/vrcwebmap-backups >/dev/null

dc exec --no-TTY postgres psql \
    --username vrcwebmap \
    --dbname vrcwebmap \
    --set ON_ERROR_STOP=1 \
    --command "CREATE TABLE backup_probe(value text NOT NULL); INSERT INTO backup_probe VALUES ('generation-a');" \
    >/dev/null

dc run --rm --no-deps db-backup backup

list_output=$(dc run --rm --no-deps db-backup list)
echo "$list_output" | grep 'backups/vrcwebmap-' >/dev/null ||
    fail "list did not return a complete backup"

[ "$(count_objects '*.dump')" -eq 1 ] || fail "expected one dump"
[ "$(count_objects '*.sha256')" -eq 1 ] || fail "expected one checksum"
[ "$(count_objects '*.json')" -eq 1 ] || fail "expected one manifest"

latest_key=$(dc run --rm --no-deps db-backup resolve latest)
echo "$latest_key" | grep -E '^backups/vrcwebmap-[0-9]{8}T[0-9]{6}Z\.dump$' >/dev/null ||
    fail "latest did not resolve to a dump key"

# manifestがないobjectは、不完全世代として一覧とlatest解決から除外します。
printf 'incomplete' >"$script_dir/incomplete.dump"
dc run --rm --no-deps \
    --volume "$script_dir:/test-data:ro" \
    minio-client cp /test-data/incomplete.dump \
    local/vrcwebmap-backups/backups/vrcwebmap-99991231T235959Z.dump >/dev/null
rm -f "$script_dir/incomplete.dump"

incomplete_list=$(dc run --rm --no-deps db-backup list)
echo "$incomplete_list" | grep '99991231T235959Z' &&
    fail "incomplete generation appeared in list"
[ "$(dc run --rm --no-deps db-backup resolve latest)" = "$latest_key" ] ||
    fail "incomplete generation changed latest"

generation=1
while [ "$generation" -le 5 ]; do
    sleep 1
    dc run --rm --no-deps db-backup backup >/dev/null
    generation=$((generation + 1))
done

[ "$(count_objects '*.json')" -eq 4 ] || fail "retention did not keep four manifests"
[ "$(count_objects '*.sha256')" -eq 4 ] || fail "retention did not keep four checksums"
# 不完全dumpはretention対象外なので、完全4世代と不完全1objectが残ります。
[ "$(count_objects '*.dump')" -eq 5 ] || fail "retention removed or added unexpected dumps"

echo "backup/list/resolve/retention integration passed"

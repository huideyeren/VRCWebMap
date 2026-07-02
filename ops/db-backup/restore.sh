#!/bin/sh
set -eu

EX_USAGE=64
EX_RESTART=71

selector=${1:-}
[ "$#" -eq 1 ] && [ -n "$selector" ] || {
    echo "usage: $0 <latest|object-key>" >&2
    exit "$EX_USAGE"
}

script_dir=$(CDPATH= cd -- "$(dirname "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_dir/../.." && pwd)
cd "$repository_root"

[ -f .env.backup ] || {
    echo "restore: .env.backup was not found in $repository_root" >&2
    exit "$EX_USAGE"
}

dc() {
    docker compose \
        --env-file .env.backup \
        --profile backup \
        "$@"
}

# backend停止前にlatestを不変のobject keyへ解決し、対象世代を表示します。
resolved_key=$(dc run --rm db-backup resolve "$selector") || exit "$?"
[ -n "$resolved_key" ] || {
    echo "restore: backup key could not be resolved" >&2
    exit "$EX_USAGE"
}

echo "Restore target:"
dc run --rm db-backup describe "$resolved_key" || exit "$?"
echo
echo "Type vrcwebmap to restore $resolved_key"
IFS= read -r confirmation
[ "$confirmation" = "vrcwebmap" ] || {
    echo "restore: confirmation did not match; no services were stopped" >&2
    exit "$EX_USAGE"
}

running_services=$(dc ps --status running --services backend) || exit "$EX_USAGE"
backend_was_running=false
if echo "$running_services" | grep -qx 'backend'; then
    backend_was_running=true
    dc stop backend || exit "$EX_USAGE"
fi

if dc run --rm db-backup restore "$resolved_key"; then
    if [ "$backend_was_running" = "true" ]; then
        dc up --detach backend || {
            echo "restore: database restore succeeded, but backend restart failed" >&2
            exit "$EX_RESTART"
        }
    fi
    echo "restore: completed successfully"
else
    restore_status=$?
    echo "restore: failed; backend remains stopped" >&2
    exit "$restore_status"
fi

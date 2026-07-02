#!/bin/sh
set -eu

case "${1:-}" in
    backup|list|resolve|describe|restore)
        echo "db-backup: command is not implemented yet: $1" >&2
        exit 64
        ;;
    "")
        echo "usage: db-backup <backup|list|resolve|describe|restore>" >&2
        exit 64
        ;;
    *)
        # image smoke testsと運用時の診断コマンドを、同じclient imageで実行できるようにします。
        exec "$@"
        ;;
esac

#!/bin/sh
set -eu

image="${1:?image is required}"

docker run --rm "$image" pg_dump --version | grep -E ' 18([. ]|$)'
docker run --rm "$image" aws --version | grep 'aws-cli/2'
docker run --rm "$image" sha256sum --help >/dev/null
docker run --rm "$image" jq --version >/dev/null

#!/usr/bin/env bash
set -ex

# Determine the runtime from the target platform provided by Docker Buildx
case "$TARGETPLATFORM" in
    "linux/arm/v7") runtime="linux-musl-arm" ;;
    "linux/arm64") runtime="linux-musl-arm64" ;;
    "linux/amd64") runtime="linux-musl-x64" ;;
    *) echo >&2 "ERROR: Unsupported target platform: $TARGETPLATFORM"; exit 1 ;;
esac

path="artifacts/recyclarr-$runtime/"

mv "$path" publish

chmod a+rx publish/recyclarr

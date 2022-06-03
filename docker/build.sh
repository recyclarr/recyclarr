#!/bin/sh
set -ex

# The download path is a bit different when using the latest release instead of a specific
# release
if [ "$RELEASE_TAG" = "latest" ]; then
    download_path="latest/download";
else
    download_path="download/$RELEASE_TAG";
fi

# Determine the runtime from the target platform provided by Docker Buildx
case "$TARGETPLATFORM" in
    "linux/arm/v7") runtime="linux-musl-arm" ;;
    "linux/arm64") runtime="linux-musl-arm64" ;;
    "linux/amd64") runtime="linux-musl-x64" ;;
    *) echo >&2 "ERROR: Unsupported target platform: $TARGETPLATFORM"; exit 1 ;;
esac

# Download and extract the recyclarr binary from the release
wget --quiet -O recyclarr.zip "https://github.com/$REPOSITORY/releases/$download_path/recyclarr-$runtime.zip"
unzip recyclarr.zip

#!/usr/bin/env bash
set -ex

# The download path is a bit different when using the latest release instead of a specific
# release
if [ "$RELEASE_TAG" = "latest" ]; then
    download_path="latest/download";
else
    download_path="download/$RELEASE_TAG";
fi

# Download and extract the recyclarr binary from the release
wget --quiet -O recyclarr.zip "https://github.com/$REPOSITORY/releases/$download_path/recyclarr-$runtime.zip"
unzip recyclarr.zip

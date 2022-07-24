#!/usr/bin/env bash
set -ex

# Do not shallow clone because gitversion needs history!
git clone -b $BUILD_FROM_BRANCH "https://github.com/$REPOSITORY.git" source

dotnet publish source/src/Recyclarr -o publish -c Release -r $runtime

#!/usr/bin/env bash
set -ex

git clone -b $BUILD_FROM_BRANCH "https://github.com/$REPOSITORY.git" source
cd source

pwsh ./ci/Publish.ps1 $runtime
cp ./publish/$runtime/recyclarr ..

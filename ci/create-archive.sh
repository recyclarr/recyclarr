#!/usr/bin/env bash
# Archives each runtime directory under a publish directory: zip for Windows, tar.xz otherwise.
# Usage: ci/create-archive.sh <publish-dir> <output-dir> [runtime-dir-name]
set -euo pipefail

publish_dir="${1:?publish directory is required}"
output_dir="${2:?output directory is required}"

if (($# >= 3)); then
    targets=("$3")
else
    targets=()
    for dir in "$publish_dir"/*/; do
        targets+=("$(basename "$dir")")
    done
fi

mkdir -p "$output_dir"
output_dir="$(cd "$output_dir" && pwd)"

for dir in "${targets[@]}"; do
    archive_name="recyclarr-$dir"
    pushd "$publish_dir/$dir" >/dev/null
    if [[ $dir == win-* ]]; then
        echo "> Zipping: $dir"
        zip -r "$output_dir/$archive_name.zip" ./*
    else
        echo "> Tarballing: $dir"
        tar -cJv --owner=0 --group=0 -f "$output_dir/$archive_name.tar.xz" -- *
    fi
    popd >/dev/null
done

#!/usr/bin/env bash
# Archives each runtime directory under a publish directory: zip for Windows, tar.xz otherwise.
# Runtimes are archived in parallel; each job's output is printed once all jobs finish.
# Usage: ci/create-archive.sh <publish-dir> <output-dir> [runtime-dir-name]
set -euo pipefail

# Both executables in a runtime embed much of the same runtime code. Only a single xz block with
# the -9 preset's 64 MiB dictionary reaches across them; multithreaded xz splits the input into
# independent blocks and grows the archive by about 60%. Parallelism therefore comes from
# archiving runtimes concurrently, each single-threaded (about 674 MiB of memory per job).
export XZ_OPT="-T1 -9"

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

log_dir="$(mktemp -d)"
trap 'rm -rf "$log_dir"' EXIT

archive() {
    local dir="$1"
    local archive_name="recyclarr-$dir"
    cd "$publish_dir/$dir"
    if [[ $dir == win-* ]]; then
        echo "> Zipping: $dir"
        zip -r "$output_dir/$archive_name.zip" ./*
    else
        echo "> Tarballing: $dir"
        tar -cJv --owner=0 --group=0 -f "$output_dir/$archive_name.tar.xz" -- *
    fi
}

pids=()
for dir in "${targets[@]}"; do
    archive "$dir" >"$log_dir/$dir.log" 2>&1 &
    pids+=($!)
done

failed=0
for i in "${!targets[@]}"; do
    status=0
    wait "${pids[$i]}" || status=$?
    cat "$log_dir/${targets[$i]}.log"
    if ((status != 0)); then
        echo "error: archiving ${targets[$i]} failed with exit code $status" >&2
        failed=1
    fi
done

exit "$failed"

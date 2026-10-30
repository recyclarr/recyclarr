#!/usr/bin/env bash
# Publishes every release executable for one runtime into a shared output directory, which
# downstream steps (smoke test, signing, archiving) treat as the release unit for that runtime.
# Usage: ci/publish.sh <runtime> [--output-dir DIR] [--configuration NAME] [--no-single-file]
#                      [--no-compress] [--ready-to-run]
set -euo pipefail

runtime="${1:?runtime is required}"
shift
output_dir="publish/$runtime"
configuration=Release
single_file=true
compress=true
args=()

while (($#)); do
    case "$1" in
        --output-dir) output_dir="$2"; shift ;;
        --configuration) configuration="$2"; shift ;;
        --no-single-file) single_file=false ;;
        --no-compress) compress=false ;;
        --ready-to-run)
            args+=(
                -p:PublishReadyToRunShowWarnings=true
                -p:PublishReadyToRunComposite=true
                -p:TieredCompilation=false
            )
            ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
    shift
done

if [[ $single_file == true ]]; then
    args+=(
        --self-contained=true
        -p:PublishSingleFile=true
        -p:IncludeNativeLibrariesForSelfExtract=true
    )
else
    args+=(--self-contained=false)
fi

# Workaround for dotnet/runtime#112167: EnableCompressionInSingleFile causes intermittent
# AccessViolationException on macOS ARM64. Disable until the fix is backported to .NET 10.
if [[ $compress == true && $runtime != osx-arm64 ]]; then
    args+=(-p:EnableCompressionInSingleFile=true)
fi

echo "Extra Args: ${args[*]}"

for project in src/Recyclarr.Cli src/Recyclarr.Server; do
    echo "> Publishing: $project"
    dotnet publish "$project" \
        --output "$output_dir" \
        --configuration "$configuration" \
        --runtime "$runtime" \
        "${args[@]}"
done

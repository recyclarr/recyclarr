#!/usr/bin/env bash
# Runs Recyclarr in a container after starting the compose services it syncs against.
# Arguments are passed to the recyclarr container.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
compose_file="$repo_root/compose.yaml"
config_dir="${RECYCLARR_CONFIG_DIR:?RECYCLARR_CONFIG_DIR is not set (set it with mise)}"
: "${RECYCLARR_API_KEY:?RECYCLARR_API_KEY is not set}"

# The image packages prebuilt binaries for the Docker host's architecture.
docker_arch="$(docker info --format '{{.Architecture}}')"
case "$docker_arch" in
    x86_64 | amd64) arch=x64 ;;
    aarch64 | arm64) arch=arm64 ;;
    *) echo "Unsupported Docker architecture: $docker_arch" >&2; exit 1 ;;
esac
"$repo_root/ci/publish.sh" "linux-musl-$arch" --no-single-file \
    --output-dir "$repo_root/publish/linux-musl-$arch"

DOCKER_UID="$(id -u)"
DOCKER_GID="$(id -g)"
export DOCKER_UID DOCKER_GID

# Ensure the bind mount directory exists with correct ownership before the container starts.
# If Docker creates it first, it ends up owned by root and the container (running as
# the current user) can't write to it.
mkdir -p "$config_dir"

# BSD stat (macOS) and GNU stat (Linux) take different format flags.
if [[ $(uname) == Darwin ]]; then
    owner="$(stat -f '%u:%g' "$config_dir")"
else
    owner="$(stat -c '%u:%g' "$config_dir")"
fi
if [[ $owner != "$DOCKER_UID:$DOCKER_GID" ]]; then
    echo "Fixing $config_dir ownership ($owner -> $DOCKER_UID:$DOCKER_GID)"
    sudo chown -R "$DOCKER_UID:$DOCKER_GID" "$config_dir"
fi

docker compose -f "$compose_file" up -d

echo "ntfy: http://localhost:8090/recyclarr"

docker compose -f "$compose_file" --profile recyclarr run --rm --build recyclarr "$@"

#!/usr/bin/env bash
set -euo pipefail

# Recyclarr 8 ran CLI commands passed as container arguments. The image now runs only the server,
# so those arguments get a pointer to `docker exec` instead of a confusing "not found" from tini.
# Any other command (the default recyclarr-server, sh, bash) runs as given.
case "${1:-}" in
sync | list | delete | config | migrate | state | -*)
  cat >&2 <<EOF
Container arguments no longer run Recyclarr CLI commands; the image runs the Recyclarr server.
Start the container without arguments, then run CLI commands inside it:

  docker exec <container> recyclarr $*

See https://recyclarr.dev/guide/upgrade-guide/v9.0/#docker-run-args
EOF
  exit 1
  ;;
esac

exec "$@"

#!/usr/bin/env bash
# Records the README sync demo to .github/assets/sync-preview.webp.
# VHS has no WebP output, so it writes lossless PNG frames and ffmpeg encodes them; WebP is a
# fraction of the GIF size for scrolling terminal output. Encoding VHS's MP4 instead adds
# compression noise and its 25 fps rate, which inflates the WebP.
set -euo pipefail

cd "$(dirname "$0")/.."

work=/tmp/recyclarr-demo
rm -rf "$work/frames"
mkdir -p "$work/config"
cp -r .github/assets/demo-config/. "$work/config/"

trap 'docker compose stop radarr_develop1 >/dev/null' EXIT
docker compose up -d --wait radarr_develop1
until curl -sf -o /dev/null -H 'X-Api-Key: testkey' http://localhost:7890/api/v3/system/status; do
    sleep 2
done

dotnet build src/Recyclarr.Cli -v q

# Warm-up run fetches guide data so the recording shows only the sync itself
RECYCLARR_CONFIG_DIR="$work/config" src/Recyclarr.Cli/bin/Debug/net10.0/recyclarr sync --preview

vhs .github/assets/sync-preview.tape
# VHS writes text and cursor layers per frame inside the tape's padding; composite them and
# restore the padding with the theme background (Catppuccin Mocha).
ffmpeg -loglevel error -y \
    -framerate 10 -i "$work/frames/frame-text-%05d.png" \
    -framerate 10 -i "$work/frames/frame-cursor-%05d.png" \
    -filter_complex "[0][1]overlay,pad=1200:700:(ow-iw)/2:(oh-ih)/2:color=0x1e1e2e,format=yuv420p" \
    -c:v libwebp_anim -q:v 60 -loop 0 .github/assets/sync-preview.webp

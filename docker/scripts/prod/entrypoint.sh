#!/bin/sh
set -e

userspec="$PUID:$PGID"

chown "$userspec" "$RECYCLARR_APP_DATA"

if [ ! -f "$RECYCLARR_APP_DATA/recyclarr.yml" ]; then
    su-exec "$userspec" recyclarr create-config
fi

# If the script has any arguments, invoke the CLI instead. This allows the image to be used as a CLI
# with something like:
#
# ```
# docker run --rm -v ./config:/config ghcr.io/recyclarr/recyclarr sonarr
# ```
#
if [ "$#" -gt 0 ]; then
    su-exec "$userspec" recyclarr "$@"
else
    echo "Creating crontab file..."
    echo "$CRON_SCHEDULE su-exec \"$userspec\" /cron.sh" | crontab -

    crontab -l

    echo "Starting cron daemon..."
    exec crond -f
fi

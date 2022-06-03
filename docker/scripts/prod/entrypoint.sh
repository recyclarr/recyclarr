#!/bin/sh
set -e

if [ ! -f "$RECYCLARR_APP_DATA/recyclarr.yml" ]; then
    su-exec recyclarr recyclarr create-config
fi

# If the script has any arguments, invoke the CLI instead. This allows the image to be used as a CLI
# with something like:
#
# ```
# docker run --rm -v ./config:/config ghcr.io/recyclarr/recyclarr sonarr
# ```
#
if [ "$#" -gt 0 ]; then
    su-exec recyclarr recyclarr "$@"
else
    echo "Creating crontab file..."
    echo "$CRON_SCHEDULE /cron.sh" | crontab -u recyclarr -

    crontab -l -u recyclarr

    echo "Starting cron daemon..."
    exec crond -f
fi

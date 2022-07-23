#!/bin/sh
set -e

# If the script has any arguments, invoke the CLI instead. This allows the image to be used as a CLI
# with something like:
#
# ```
# docker run --rm -v ./config:/config ghcr.io/recyclarr/recyclarr sonarr
# ```
#
if [ "$#" -gt 0 ]; then
    recyclarr "$@"
else
    echo "Creating crontab file..."
    echo "$CRON_SCHEDULE /cron.sh" | crontab -

    crontab -l

    echo "Starting cron daemon..."
    exec crond -f
fi

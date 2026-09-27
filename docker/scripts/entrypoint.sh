#!/usr/bin/env bash
set -e

config=/config/recyclarr.yml
if [[ "$RECYCLARR_CREATE_CONFIG" = true && ! -f "$config" ]]; then
  echo "Creating default recyclarr.yml file..."
  recyclarr config create
fi

# If the script has any arguments, invoke the CLI instead
if [ "$#" -gt 0 ]; then
    exec recyclarr "$@"
else
    echo "Starting cron schedule using: $CRON_SCHEDULE"
    echo "$CRON_SCHEDULE /cron.sh" > /tmp/crontab
    exec supercronic -passthrough-logs -no-reap /tmp/crontab
fi

#!/usr/bin/env bash
set -e

config=/config/recyclarr.yml
if [[ "$RECYCLARR_CREATE_CONFIG" = true && ! -f "$config" ]]; then
  echo "Creating default recyclarr.yml file..."
  recyclarr create-config
fi

# If the script has any arguments, invoke the CLI instead
if [ "$#" -gt 0 ]; then
    recyclarr "$@"
else
    echo "Starting cron schedule using: $CRON_SCHEDULE"
    echo "$CRON_SCHEDULE /cron.sh" > /tmp/crontab
    supercronic -passthrough-logs /tmp/crontab
fi

#!/usr/bin/env bash
set -e

if [[ ! -z ${PUID+x} ]]; then
    echo 'PUID is no longer supported. Use `--user` instead.'
    exit 1
fi

if [[ ! -z ${PGID+x} ]]; then
    echo 'PGID is no longer supported. Use `--user` instead.'
    exit 1
fi

# If the script has any arguments, invoke the CLI instead
if [ "$#" -gt 0 ]; then
    recyclarr "$@"
else
    echo "Starting cron schedule..."
    echo "$CRON_SCHEDULE /cron.sh" > /tmp/crontab
    supercronic -passthrough-logs /tmp/crontab
fi

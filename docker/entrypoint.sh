#!/bin/sh
set -e

# If the script has any arguments, invoke the CLI instead.
# This allows the image to be used as a CLI with something like "docker run --rm -it -v ./trash.yml:/config/trash.yml rcdailey/trash-updater sonarr".
if [ "$#" -gt 0 ]; then
	/usr/local/bin/trash "$@"
else
	schedule=${CRON_SCHEDULE:-@daily}
	sonarr_schedule=${CRON_SCHEDULE_SONARR:-$schedule}
	radarr_schedule=${CRON_SCHEDULE_RADARR:-$schedule}

	config_file=${CONFIG_FILE:-/config/trash.yml}

	echo "Creating crontab file..."
	echo "$sonarr_schedule /usr/local/bin/trash sonarr --config $config_file" > /var/spool/cron/crontabs/root
	echo "$radarr_schedule /usr/local/bin/trash radarr --config $config_file" >> /var/spool/cron/crontabs/root

	echo "Starting cron daemon..."
	exec crond -f
fi

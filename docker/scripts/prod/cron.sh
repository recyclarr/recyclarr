#!/bin/sh
set -e

echo
echo "-------------------------------------------------------------"
echo " Executing Cron Tasks: $(date)"
echo "-------------------------------------------------------------"
echo

recyclarr sonarr
recyclarr radarr

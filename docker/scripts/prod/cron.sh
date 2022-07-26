#!/usr/bin/env bash
echo
echo "-------------------------------------------------------------"
echo " Executing Cron Tasks: $(date)"
echo "-------------------------------------------------------------"
echo

recyclarr sonarr
recyclarr radarr

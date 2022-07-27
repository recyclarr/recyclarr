#!/usr/bin/env bash
echo
echo "-------------------------------------------------------------"
echo " Executing Tasks: $(date)"
echo "-------------------------------------------------------------"

echo
echo ">>> Sonarr <<<"
echo
recyclarr sonarr

echo
echo ">>> Radarr <<<"
echo
recyclarr radarr

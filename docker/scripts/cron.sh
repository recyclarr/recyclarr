#!/usr/bin/env bash
echo
echo "-------------------------------------------------------------"
echo " Executing Tasks: $(date)"
echo "-------------------------------------------------------------"

log_arg="--log${RECYCLARR_LOG_LEVEL:+ $RECYCLARR_LOG_LEVEL}"
recyclarr sync $log_arg

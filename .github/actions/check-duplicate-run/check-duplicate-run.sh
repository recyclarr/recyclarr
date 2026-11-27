#!/usr/bin/env bash
set -euo pipefail

# Inputs (environment): GH_TOKEN, plus the GITHUB_* variables Actions sets for every run.

mainline_or_tag=false
if [[ $GITHUB_REF_TYPE == tag || $GITHUB_REF_NAME == master ]]; then
  mainline_or_tag=true
fi

if [[ $GITHUB_EVENT_NAME == workflow_dispatch || $mainline_or_tag == true ]]; then
  echo "full_build=true" >>"$GITHUB_OUTPUT"
else
  echo "full_build=false" >>"$GITHUB_OUTPUT"
fi

# Always run for non-push events (prioritize PR builds, workflow_dispatch, etc.)
if [[ $GITHUB_EVENT_NAME != push ]]; then
  echo "should_run=true" >>"$GITHUB_OUTPUT"
  echo "Event type: $GITHUB_EVENT_NAME - allowing run"
  exit 0
fi

# Merged commits on master have PRs too, so only feature-branch pushes are deduplicated.
if [[ $mainline_or_tag == true ]]; then
  echo "should_run=true" >>"$GITHUB_OUTPUT"
  echo "Push to $GITHUB_REF_NAME - allowing run"
  exit 0
fi

# A failed lookup allows the run rather than skipping a build.
echo "Checking for PRs associated with commit $GITHUB_SHA"
pr_count=$(gh api --method GET "repos/$GITHUB_REPOSITORY/commits/$GITHUB_SHA/pulls" \
  --jq length || echo 0)

if ((pr_count > 0)); then
  echo "should_run=false" >>"$GITHUB_OUTPUT"
  echo "Found $pr_count PR(s) for this commit - skipping push event run"
else
  echo "should_run=true" >>"$GITHUB_OUTPUT"
  echo "No PRs found for this commit - allowing push event run"
fi

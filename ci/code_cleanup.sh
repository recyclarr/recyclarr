#!/usr/bin/env bash
dotnet jb cleanupcode Recyclarr.sln \
  --settings="Recyclarr.sln.DotSettings" \
  --profile="Recyclarr Cleanup" \
  --include="**.cs"

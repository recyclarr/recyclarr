#!/usr/bin/env bash
dotnet jb cleanupcode Recyclarr.slnx \
  --settings="Recyclarr.slnx.DotSettings" \
  --profile="Recyclarr Cleanup" \
  --include="**.cs"

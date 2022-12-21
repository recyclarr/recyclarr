#!/usr/bin/env bash
set -x
name="$1"

if [[ "$name" == '' ]]; then
  dirs=(*/)
else
  dirs=(.)
fi

for dir in ${dirs[@]}; do
  echo "> Extracting: $dir"
  pushd "$dir" > /dev/null
  tar xvf artifact.tar
  rm artifact.tar
  popd > /dev/null
done

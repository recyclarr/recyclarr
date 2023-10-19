#!/usr/bin/env bash
set -e

user="$1"
pass="$2"
teamId="$3"
archivePath="$4"

function submit() {
  xcrun notarytool submit --wait --no-progress -f json \
    --apple-id "$user" \
    --password "$pass" \
    --team-id "$teamId" \
    recyclarr.zip | \
    jq -r .id
}

function log() {
  xcrun notarytool log \
    --apple-id "$user" \
    --password "$pass" \
    --team-id "$teamId" \
    "$1"
}

tar -cvf recyclarr.tar -C "$(dirname "$archivePath")" "$(basename "$archivePath")"
zip recyclarr.zip recyclarr.tar
submissionId="$(submit)"
rm recyclarr.zip recyclarr.tar

if [[ -z "$submissionId" ]]; then
  exit 1
fi

echo "Submission ID: $submissionId"

until log "$submissionId"
do
  sleep 2
done

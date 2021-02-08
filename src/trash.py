import requests
# import re
# import json
# from collections import defaultdict
# from dataclasses import dataclass
# from enum import Enum
from packaging import version # pip install packaging

from trash import guide
from trash.api.sonarr import Sonarr
from trash.guide import anime, utils
from trash.cmd import setup_and_parse_args
from trash.logger import Logger

####################################################################################################

try:
    args = setup_and_parse_args()
    logger = Logger(args)
    sonarr = Sonarr(args, logger)

    profiles = anime.parse_markdown(logger)

    # A few false-positive profiles are added sometimes. We filter these out by checking if they
    # actually have meaningful data attached to them, such as preferred terms. If they are mostly empty,
    # we remove them here.
    utils.filter_profiles(profiles)

    if args.preview:
        utils.print_terms_and_scores(profiles)
        exit(0)

    # Since this script requires a specific version of v3 Sonarr that implements name support for
    # release profiles, we perform that version check here and bail out if it does not meet a minimum
    # required version.
    minimum_version = version.parse('3.0.4.1098')
    version = sonarr.get_version()
    if version < minimum_version:
        print(f'ERROR: Your Sonarr version ({version}) does not meet the minimum required version of {minimum_version} to use this script.')
        exit(1)

    # If tags were provided, ensure they exist. Tags that do not exist are added first, so that we
    # may specify them with the release profile request payload.
    tag_ids = []
    if args.tags:
        tags = sonarr.get_tags()
        tags = sonarr.create_missing_tags(tags, args.tags[:])
        logger.debug(f'Tags JSON: {tags}')

        # Get a list of IDs that we can pass along with the request to update/create release
        # profiles
        tag_ids = [t['id'] for t in tags if t['label'] in args.tags]
        logger.debug(f'Tag IDs: {tag_ids}')

    # Obtain all of the existing release profiles first. If any were previously created by our script
    # here, we favor replacing those instead of creating new ones, which would just be mostly duplicates
    # (but with some differences, since there have likely been updates since the last run).
    existing_profiles = sonarr.get_release_profiles()

    for name, profile in profiles.items():
        new_profile_name = f'[Trash] Anime - {name}'
        profile_to_update = guide.utils.find_existing_profile(new_profile_name, existing_profiles)

        if profile_to_update:
            print(f'Updating existing profile: {new_profile_name}')
            sonarr.update_existing_profile(profile_to_update, profile, tag_ids)
        else:
            print(f'Creating new profile: {new_profile_name}')
            sonarr.create_release_profile(new_profile_name, profile, tag_ids)

except requests.exceptions.HTTPError as e:
    print(e)
    if error_msg := Sonarr.get_error_message(e.response):
        print(f'Response Message: {error_msg}')
    exit(1)

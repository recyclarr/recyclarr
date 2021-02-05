import requests
import re
import json
from collections import defaultdict
import argparse
# from dataclasses import dataclass
from enum import Enum
from packaging import version # pip install packaging


# Argparse setup
argparser = argparse.ArgumentParser(description='Automatically mirror TRaSH guides to your *darr instance.')
argparser.add_argument('--preview', help='Only display the processed markdown results and nothing else.',
    action='store_true')
argparser.add_argument('--debug', help='Display additional logs useful for development/debug purposes',
    action='store_true')
argparser.add_argument('base_uri', help='The base URL for your Sonarr instance, for example `http://localhost:8989`.')
argparser.add_argument('api_key', help='Your API key.')
args = argparser.parse_args()

Filter = Enum('FilterType', 'Preferred Required Ignored')

header_regex = re.compile(r'^(#+)\s([\w\s\d]+)\s*$')
score_regex = re.compile(r'score.*?\[([-\d]+)\]', re.IGNORECASE)
# included_preferred_regex = re.compile(r'include preferred', re.IGNORECASE)
# not_regex = re.compile(r'not', re.IGNORECASE)
filter_regexes = (
    (Filter.Ignored, re.compile(r'must not contain', re.IGNORECASE)),
    (Filter.Preferred, re.compile(r'preferred', re.IGNORECASE)),
)

# SONARR API STUFFS
base_uri = f'{args.base_uri}/api/v3'
key = f'?apikey={args.api_key}'

# --------------------------------------------------------------------------------------------------
def get_trash_anime_markdown():
    trash_anime_markdown_url = 'https://raw.githubusercontent.com/TRaSH-/Guides/master/docs/Sonarr/V3/Sonarr-Release-Profile-RegEx-Anime.md'
    response = requests.get(trash_anime_markdown_url)
    return response.content.decode('utf8')

# --------------------------------------------------------------------------------------------------
def parse_filter(line):
    for rx in filter_regexes:
        if rx[1].search(line):
            return rx[0]

    return None

# --------------------------------------------------------------------------------------------------
def debug_print(text):
    if args.debug:
        print(text)

# --------------------------------------------------------------------------------------------------
class ProfileData:
    def __init__(self):
        self.preferred = defaultdict(list)
        self.required = []
        self.ignored = []
        # We use 'none' here to represent no explicit mention of the "include preferred" string
        # found in the markdown. We use this to control whether or not the corresponding profile
        # section gets printed in the first place.
        self.include_preferred_when_renaming = None

def parse_markdown(markdown_content):
    class state:
        results = defaultdict(ProfileData)
        profile_name = None
        score = None
        filter = None
        bracket_depth = 0

    for line in markdown_content.splitlines():
        # Header processing
        if match := header_regex.search(line):
            header_depth = len(match.group(1))
            header_text = match.group(2)

            # Profile name (always reset previous state here)
            if header_depth == 3:
                state.score = None
                state.filter = Filter.Preferred
                state.profile_name = header_text
                debug_print(f'New Profile: {header_text}')
            # Filter type for provided regexes
            elif header_depth == 4:
                state.filter = parse_filter(header_text)
                if state.filter:
                    debug_print(f'  Filter Set: {state.filter}')

        # Lines we always look for
        elif line.startswith('```'):
            state.bracket_depth = 1 - state.bracket_depth

        # Filter-based line processing
        elif state.profile_name:
            profile = state.results[state.profile_name]
            lower_line = line.lower()
            if 'include preferred' in lower_line:
                profile.include_preferred_when_renaming = 'not' not in lower_line
                debug_print(f'  Include preferred found: {profile.include_preferred_when_renaming}, {lower_line}')
            elif state.filter == Filter.Preferred:
                if match := score_regex.search(line):
                    # bracket_depth = 0
                    state.score = int(match.group(1))
                elif state.bracket_depth:
                    if state.score is not None:
                        debug_print(f'  [Preferred] Score: {state.score}, Term: {line}')
                        profile.preferred[state.score].append(line)
            elif state.filter == Filter.Ignored:
                if state.bracket_depth:
                    # Sometimes a comma is present at the end of these regexes, because when it's
                    # pasted into Sonarr it acts as a delimiter. However, when using them with the
                    # API we do not need them.
                    profile.ignored.append(line.rstrip(','))

    debug_print('\n\n')
    return state.results

# --------------------------------------------------------------------------------------------------
def filter_profiles(profiles):
    for name in list(profiles.keys()):
        profile = profiles[name]
        if not len(profile.required) and not len(profile.ignored) and not len(profile.preferred):
            del profiles[name]

# --------------------------------------------------------------------------------------------------
def print_terms_and_scores(profiles):
    for name, profile in profiles.items():
        print(name)

        if profile.include_preferred_when_renaming is not None:
            print('  Include Preferred when Renaming?')
            print('    ' + ('CHECKED' if profile.include_preferred_when_renaming else 'NOT CHECKED'))
            print('')

        if len(profile.required):
            print('  Must Contain:')
            for term in profile.required:
                print(f'    {term}')
            print('')

        if len(profile.ignored):
            print('  Must Not Contain:')
            for term in profile.ignored:
                print(f'    {term}')
            print('')

        if len(profile.preferred):
            print('  Preferred:')
            for score, terms in profile.preferred.items():
                for term in terms:
                    print(f'    {score:<10} {term}')

        print('')

# --------------------------------------------------------------------------------------------------
def success_status_code(response):
    return response.status_code >= 200 and response.status_code < 300

# --------------------------------------------------------------------------------------------------
def get_error_message(response: requests.Response):
    response_body = json.dumps(response.content.decode('utf8'))
    if type(response_body) is list and len(response_body) > 0:
        return response[0]['errorMessage']
    return None

# --------------------------------------------------------------------------------------------------
def sonarr_request(method, endpoint, data=None):
    dispatch = {
        'put': requests.put,
        'get': requests.get,
        'post': requests.post,
    }

    complete_uri = base_uri + endpoint + key
    r = dispatch.get(method)(complete_uri, json.dumps(data))
    r.raise_for_status()
    return json.loads(r.content)

# --------------------------------------------------------------------------------------------------
def sonarr_get_version():
    body = sonarr_request('get', '/system/status')
    return version.parse(body['version'])

# --------------------------------------------------------------------------------------------------
def sonarr_create_release_profile(profile_name: str, profile: ProfileData):
    json_preferred = []
    for score, terms in profile.preferred.items():
        for term in terms:
            json_preferred.append({"key": term, "value": score})

    data = {
        'name': profile_name,
        'enabled': True,
        'required': ','.join(profile.required),
        'ignored': ','.join(profile.ignored),
        'preferred': json_preferred,
        'includePreferredWhenRenaming': profile.include_preferred_when_renaming,
        'tags': [],
        'indexerId': 0
    }

    sonarr_request('post', '/releaseprofile', data)

# --------------------------------------------------------------------------------------------------
def sonarr_get_release_profiles():
    return sonarr_request('get', '/releaseprofile')

# --------------------------------------------------------------------------------------------------
def find_existing_profile(profile_name, existing_profiles):
    for p in existing_profiles:
        if p.get('name') == new_profile_name:
            return p
    return None

# --------------------------------------------------------------------------------------------------
def sonarr_update_existing_profile(existing_profile, profile):
    profile_id = existing_profile['id']
    debug_print(f'update existing profile with id {profile_id}')

    # Create the release profile
    json_preferred = []
    for score, terms in profile.preferred.items():
        for term in terms:
            json_preferred.append({"key": term, "value": score})

    existing_profile['required'] = ','.join(profile.required)
    existing_profile['ignored'] = ','.join(profile.ignored)
    existing_profile['preferred'] = json_preferred
    existing_profile['includePreferredWhenRenaming'] = profile.include_preferred_when_renaming

    sonarr_request('put', f'/releaseprofile/{profile_id}', existing_profile)

####################################################################################################

try:
    profiles = parse_markdown(get_trash_anime_markdown())

    # A few false-positive profiles are added sometimes. We filter these out by checking if they
    # actually have meaningful data attached to them, such as preferred terms. If they are mostly empty,
    # we remove them here.
    filter_profiles(profiles)

    if args.preview:
        print_terms_and_scores(profiles)
        exit(0)

    # Since this script requires a specific version of v3 Sonarr that implements name support for
    # release profiles, we perform that version check here and bail out if it does not meet a minimum
    # required version.
    minimum_version = version.parse('3.0.4.1098')
    version = sonarr_get_version()
    if version < minimum_version:
        print(f'ERROR: Your Sonarr version ({version}) does not meet the minimum required version of {minimum_version} to use this script.')
        exit(1)

    # Obtain all of the existing release profiles first. If any were previously created by our script
    # here, we favor replacing those instead of creating new ones, which would just be mostly duplicates
    # (but with some differences, since there have likely been updates since the last run).
    existing_profiles = sonarr_get_release_profiles()

    for name, profile in profiles.items():
        new_profile_name = f'[Trash] Anime - {name}'
        profile_to_update = find_existing_profile(new_profile_name, existing_profiles)

        if profile_to_update:
            print(f'Updating existing profile: {new_profile_name}')
            sonarr_update_existing_profile(profile_to_update, profile)
        else:
            print(f'Creating new profile: {new_profile_name}')
            sonarr_create_release_profile(new_profile_name, profile)

except requests.exceptions.HTTPError as e:
    print(e)
    if error_msg := get_error_message(e.response):
        print(f'Response Message: {error_msg}')
    exit(1)

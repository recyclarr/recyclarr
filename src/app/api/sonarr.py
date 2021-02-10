import requests
import json
from packaging import version # pip install packaging
from copy import deepcopy

from .server import Server
from ..profile_data import ProfileData

class Sonarr(Server):
    # --------------------------------------------------------------------------------------------------
    def __init__(self, args, logger):
        base_uri = f'{args.base_uri}/api/v3'
        key = f'?apikey={args.api_key}'
        self.logger = logger
        super().__init__(base_uri, key)

        if not args.base_uri or not args.api_key:
            raise ValueError('--base-uri and --api-key are required arguments when not using --preview')

        self.do_version_check()

    # --------------------------------------------------------------------------------------------------
    @staticmethod
    def get_error_message(response: requests.Response):
        content = json.loads(response.content)
        if len(content) > 0:
            if type(content) is list:
                return content[0]['errorMessage']
            elif type(content) is dict and 'message' in content:
                return content['message']
        return None

    # --------------------------------------------------------------------------------------------------
    def get_version(self):
        body = self.request('get', '/system/status')
        return version.parse(body['version'])

    # --------------------------------------------------------------------------------------------------
    def create_release_profile(self, profile_name: str, profile: ProfileData, tag_ids: list):
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
            'tags': tag_ids,
            'indexerId': 0
        }

        self.request('post', '/releaseprofile', data)

    # --------------------------------------------------------------------------------------------------
    def get_release_profiles(self):
        return self.request('get', '/releaseprofile')

    # --------------------------------------------------------------------------------------------------
    def update_existing_profile(self, existing_profile, profile, tag_ids: list):
        profile_id = existing_profile['id']
        self.logger.debug(f'update existing profile with id {profile_id}')

        # Create the release profile
        json_preferred = []
        for score, terms in profile.preferred.items():
            for term in terms:
                json_preferred.append({"key": term, "value": score})

        existing_profile['required'] = ','.join(profile.required)
        existing_profile['ignored'] = ','.join(profile.ignored)
        existing_profile['preferred'] = json_preferred
        existing_profile['includePreferredWhenRenaming'] = profile.include_preferred_when_renaming

        if len(tag_ids) > 0:
            existing_profile['tags'] = tag_ids

        self.request('put', f'/releaseprofile/{profile_id}', existing_profile)

    # --------------------------------------------------------------------------------------------------
    def get_tags(self):
        return self.request('get', '/tag')

    # --------------------------------------------------------------------------------------------------
    def create_missing_tags(self, current_tags_json, new_tags: list):
        for t in current_tags_json:
            try:
                new_tags.remove(t['label'])
            except ValueError:
                # The tag is not in the list specified by the user; ignore and continue
                pass

        # Anything still left in `new_tags` represents tags we need to add in Sonarr
        for t in new_tags:
            self.logger.debug(f'Creating tag: {t}')
            r = self.request('post', '/tag', {'label': t})
            current_tags_json.append(r)

        return current_tags_json

    # --------------------------------------------------------------------------------------------------
    def do_version_check(self):
        # Since this script requires a specific version of v3 Sonarr that implements name support for
        # release profiles, we perform that version check here and bail out if it does not meet a minimum
        # required version.
        minimum_version = version.parse('3.0.4.1098')
        sonarr_version = self.get_version()
        if sonarr_version < minimum_version:
            raise RuntimeError(f'Your Sonarr version ({sonarr_version}) does not meet the minimum required version of {minimum_version} to use this script.')
            exit(1)

    # --------------------------------------------------------------------------------------------------
    # GET /qualitydefinition
    def get_quality_definition(self):
        return self.request('get', '/qualitydefinition')

    # --------------------------------------------------------------------------------------------------
    # PUT /qualityDefinition/update
    def update_quality_definition(self, sonarr_definition, guide_definition):
        new_definition = deepcopy(sonarr_definition)
        for quality, min, max in guide_definition:
            entry = self.find_quality_definition_entry(new_definition, quality)
            if not entry:
                print(f'WARN: Quality definition lacks entry for {quality}; it will be skipped.')
                continue
            entry['minSize'] = min
            entry['maxSize'] = max

        self.request('put', '/qualityDefinition/update', new_definition)

    # --------------------------------------------------------------------------------------------------
    def find_quality_definition_entry(self, new_definition, quality):
        for entry in new_definition:
            if entry.get('quality').get('name') == quality:
                return entry

        return None
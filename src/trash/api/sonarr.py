import requests
import json
from packaging import version # pip install packaging

from .server import Server
from ..profile_data import ProfileData

class Sonarr(Server):
    # --------------------------------------------------------------------------------------------------
    def __init__(self, args, logger):
        base_uri = f'{args.base_uri}/api/v3'
        key = f'?apikey={args.api_key}'
        self.logger = logger
        super().__init__(base_uri, key)

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

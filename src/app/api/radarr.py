import requests
import json
from copy import deepcopy

from app.api.server import Server, TrashHttpError
from app.trash_error import TrashError

class RadarrHttpError(TrashHttpError):
    @staticmethod
    def get_error_message(response: requests.Response):
        content = json.loads(response.content)
        if len(content) > 0:
            if type(content) is list:
                return content[0]['errorMessage']
            elif type(content) is dict and 'message' in content:
                return content['message']
        return None

    def __str__(self):
        msg = f'HTTP Response Error [Status Code {self.response.status_code}] [URI: {self.response.url}]'
        if error_msg := RadarrHttpError.get_error_message(self.response):
            msg += f'\n  Response Message: {error_msg}'
        return msg

class Radarr(Server):
    # --------------------------------------------------------------------------------------------------
    def __init__(self, args, logger):
        if not args.base_uri or not args.api_key:
            raise TrashError('--base-uri and --api-key are required arguments when not using --preview')

        self.logger = logger

        base_uri = f'{args.base_uri}/api/v3'
        key = f'?apikey={args.api_key}'
        super().__init__(base_uri, key, RadarrHttpError)

    # --------------------------------------------------------------------------------------------------
    # GET /qualitydefinition
    def get_quality_definition(self):
        return self.request('get', '/qualitydefinition')

    # --------------------------------------------------------------------------------------------------
    # PUT /qualityDefinition/update
    def update_quality_definition(self, server_definition, guide_definition):
        new_definition = deepcopy(server_definition)
        for quality, min, max, preferred in guide_definition:
            entry = self.find_quality_definition_entry(new_definition, quality)
            if not entry:
                print(f'WARN: Quality definition lacks entry for {quality}; it will be skipped.')
                continue
            entry['minSize'] = min
            entry['maxSize'] = max
            entry['preferredSize'] = preferred

        self.request('put', '/qualityDefinition/update', new_definition)

    # --------------------------------------------------------------------------------------------------
    def find_quality_definition_entry(self, definition, quality):
        for entry in definition:
            if entry.get('quality').get('name') == quality:
                return entry

        return None
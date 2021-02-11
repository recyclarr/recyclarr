import json
import requests

class Server:
    def __init__(self, base_uri, apikey):
        self.base_uri = base_uri
        self.apikey = apikey

    def build_uri(self, endpoint):
        return self.base_uri + endpoint + self.apikey

    def request(self, method, endpoint, data=None):
        dispatch = {
            'put': requests.put,
            'get': requests.get,
            'post': requests.post,
        }

        r = dispatch.get(method)(self.build_uri(endpoint), json.dumps(data))
        r.raise_for_status()
        return json.loads(r.content)

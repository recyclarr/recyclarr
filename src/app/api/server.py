import json
import requests

from app.trash_error import TrashError

class TrashHttpError(TrashError):
    def __init__(self, response):
        self.response = response

class Server:
    dispatch = {
        'put': requests.put,
        'get': requests.get,
        'post': requests.post,
    }

    def __init__(self, base_uri, apikey, exception_strategy):
        self.base_uri = base_uri
        self.apikey = apikey
        self.exception_strategy = exception_strategy

    def build_uri(self, endpoint):
        return self.base_uri + endpoint + self.apikey

    def request(self, method, endpoint, data=None):
        r = Server.dispatch.get(method)(self.build_uri(endpoint), json.dumps(data))
        if 400 <= r.status_code < 600:
            raise self.exception_strategy(r)
        return json.loads(r.content)

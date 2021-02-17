from inspect import cleandoc

from app import cmd
from app.logic import config
from tests.mock_logger import MockLogger


def test_config_tags():
    yaml = cleandoc('''
    sonarr:
      base_uri: http://localhost:8989
      api_key: a95cc792074644759fefe3ccab544f6e
      profile:
        - type: anime
          tags:
            - anime
        - type: web-dl
          tags:
            - tv
            - series
    ''')

    args = cmd.setup_and_parse_args(['profile', 'sonarr:anime'])
    config.load_config_string(args, MockLogger(), yaml)
    assert args.base_uri == 'http://localhost:8989'
    assert args.api_key == 'a95cc792074644759fefe3ccab544f6e'
    assert args.tags == ['anime']

    args = cmd.setup_and_parse_args(['profile', 'sonarr:web-dl'])
    config.load_config_string(args, MockLogger(), yaml)
    assert args.tags == ['tv', 'series']

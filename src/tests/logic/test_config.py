from inspect import cleandoc
from pathlib import Path

from app import cmd
from app.logic import config
from tests.mock_logger import MockLogger

def test_config_load_from_file_default(mocker):
    mock_open = mocker.patch('app.logic.config.open', mocker.mock_open(read_data=''))
    mocker.patch.object(Path, 'exists', return_value=True)

    args = cmd.setup_and_parse_args(['profile', 'sonarr:anime'])
    default_root = Path(__file__).parent
    config.load_config(args, MockLogger(), default_root)

    mock_open.assert_called_once_with(default_root / 'trash.yml', 'r')

def test_config_load_from_file_args(mocker):
    mock_open = mocker.patch('app.logic.config.open', mocker.mock_open(read_data=''))
    mocker.patch.object(Path, 'exists', return_value=True)

    expected_yml_path = Path(__file__).parent.parent / 'overridden_config.yml'
    args = cmd.setup_and_parse_args(['profile', 'sonarr:anime', '--config', str(expected_yml_path)])
    config.load_config(args, MockLogger(), '.')

    mock_open.assert_called_once_with(expected_yml_path, 'r')

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

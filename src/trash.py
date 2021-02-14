import requests
from pathlib import Path
import yaml

from app.logic import sonarr
from app.cmd import setup_and_parse_args
from app.logger import Logger
from app.trash_error import TrashError

# --------------------------------------------------------------------------------------------------
def load_config(args, logger):
    if args.config_file:
        config_path = Path(args.config_file)
    else:
        # Look for `trash.yml` in the same directory as the python script.
        config_path = Path(__name__).parent / 'trash.yml'

    logger.debug(f'Using configuration file: {config_path}')

    if config_path.exists():
        config = None
        with open(config_path, 'r') as f:
            config = yaml.load(f, Loader=yaml.Loader)

        # TODO: Determine whether to use sonarr or radarr configs?
        sonarr_config = config['sonarr']

        if not args.base_uri:
            args.base_uri = sonarr_config['base_uri']

        if not args.api_key:
            args.api_key = sonarr_config['api_key']
    else:
        logger.debug('Config file could not be loaded because it does not exist')

# --------------------------------------------------------------------------------------------------
def main():
    args = setup_and_parse_args()
    logger = Logger(args)

    load_config(args, logger)

    if args.subcommand == 'profile':
        if args.type.startswith('sonarr:'):
            sonarr.process_profile(args, logger)
        elif args.type.startswith('radarr:'):
            raise TrashError('Radarr guide support is not implemented yet')

    elif args.subcommand == 'quality':
        if args.type.startswith('sonarr:'):
            sonarr.process_quality(args, logger)
        elif args.type.startswith('radarr:'):
            raise TrashError('Radarr quality support is not implemented yet')

# --------------------------------------------------------------------------------------------------
if __name__ == '__main__':
    try:
        main()
    except TrashError as e:
        print(f'ERROR: {e}')
        exit(1)

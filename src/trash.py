from pathlib import Path

from app.logic import sonarr, config
from app.cmd import setup_and_parse_args
from app.logger import Logger
from app.trash_error import TrashError

# --------------------------------------------------------------------------------------------------
def main():
    args = setup_and_parse_args()
    logger = Logger(args)

    config.load_config(args, logger, Path(__file__).parent)

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

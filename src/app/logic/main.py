from pathlib import Path

from app.logic import sonarr, config
from app.cmd import setup_and_parse_args
from app.logger import Logger
from app.trash_error import TrashError

# --------------------------------------------------------------------------------------------------
def main(root_directory):
    args = setup_and_parse_args()
    logger = Logger(args)

    config.load_config(args, logger, root_directory)

    subcommand_handlers = {
        ('sonarr', 'profile'): sonarr.process_profile,
        ('sonarr', 'quality'): sonarr.process_quality,
    }

    server_name = args.type.split(':')[0]

    try:
        subcommand_handlers[server_name, args.subcommand](args, logger)
    except KeyError:
        raise TrashError(f'{args.subcommand} support in {server_name} is not implemented yet')
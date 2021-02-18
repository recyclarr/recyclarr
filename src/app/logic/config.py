from pathlib import Path
import yaml

# --------------------------------------------------------------------------------------------------
def find_profile_by_name(config, profile_type):
    for profile in config['profile']:
        if profile['type'] == profile_type:
            return profile
    return None

# --------------------------------------------------------------------------------------------------
def load_config(args, logger, default_load_path: Path):
    if args.config_file:
        config_path = Path(args.config_file)
    else:
        # Look for `trash.yml` in the same directory as the main (entrypoint) python script.
        config_path = default_load_path / 'trash.yml'

    logger.debug(f'Using configuration file: {config_path}')

    if config_path.exists():
        with open(config_path, 'r') as f:
            config_yaml = f.read()
        load_config_string(args, logger, config_yaml)
    else:
        logger.debug('Config file could not be loaded because it does not exist')

# --------------------------------------------------------------------------------------------------
def load_config_string(args, logger, config_yaml):
    config = yaml.load(config_yaml, Loader=yaml.Loader)
    if not config:
        return

    server_name, type_name = args.type.split(':')
    server_config = config[server_name]

    if not args.base_uri:
        args.base_uri = server_config['base_uri']

    if not args.api_key:
        args.api_key = server_config['api_key']

    if args.subcommand == 'profile':
        profile = find_profile_by_name(server_config, type_name)
        if profile:
            if args.tags is None:
                args.tags = []
            args.tags.extend(t for t in profile['tags'] if t not in args.tags)

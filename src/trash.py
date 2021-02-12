import requests
import re
from pathlib import Path
import yaml

from app import guide
from app.api.sonarr import Sonarr
from app.guide import anime, utils, quality
from app.cmd import setup_and_parse_args
from app.logger import Logger

def process_sonarr_profile(args, logger):
    profiles = anime.parse_markdown(logger, anime.get_trash_anime_markdown())

    # A few false-positive profiles are added sometimes. We filter these out by checking if they
    # actually have meaningful data attached to them, such as preferred terms. If they are mostly empty,
    # we remove them here.
    utils.filter_profiles(profiles)

    if args.preview:
        utils.print_terms_and_scores(profiles)
        exit(0)

    sonarr = Sonarr(args, logger)

    # If tags were provided, ensure they exist. Tags that do not exist are added first, so that we
    # may specify them with the release profile request payload.
    tag_ids = []
    if args.tags:
        tags = sonarr.get_tags()
        tags = sonarr.create_missing_tags(tags, args.tags[:])
        logger.debug(f'Tags JSON: {tags}')

        # Get a list of IDs that we can pass along with the request to update/create release
        # profiles
        tag_ids = [t['id'] for t in tags if t['label'] in args.tags]
        logger.debug(f'Tag IDs: {tag_ids}')

    # Obtain all of the existing release profiles first. If any were previously created by our script
    # here, we favor replacing those instead of creating new ones, which would just be mostly duplicates
    # (but with some differences, since there have likely been updates since the last run).
    existing_profiles = sonarr.get_release_profiles()

    for name, profile in profiles.items():
        new_profile_name = f'[Trash] Anime - {name}'
        profile_to_update = guide.utils.find_existing_profile(new_profile_name, existing_profiles)

        if profile_to_update:
            logger.info(f'Updating existing profile: {new_profile_name}')
            sonarr.update_existing_profile(profile_to_update, profile, tag_ids)
        else:
            logger.info(f'Creating new profile: {new_profile_name}')
            sonarr.create_release_profile(new_profile_name, profile, tag_ids)

def process_sonarr_quality(args, logger):
    guide_definitions = quality.parse_markdown(logger, quality.get_markdown())

    if args.type == 'sonarr:hybrid':
        hybrid_quality_regex = re.compile(r'720|1080')
        anime = guide_definitions.get('sonarr:anime')
        nonanime = guide_definitions.get('sonarr:non-anime')
        if len(anime) != len(nonanime):
            raise RuntimeError('For some reason the anime and non-anime quality definitions are not the same length')

        logger.info('Notice: Hybrid only functions on 720/1080 qualities and uses non-anime values for the rest (e.g. 2160)')

        hybrid = []
        for i in range(len(nonanime)):
            left = nonanime[i]
            if not hybrid_quality_regex.search(left[0]):
                logger.debug('Ignored Quality: ' + left[0])
                hybrid.append(left)
            else:
                right = None
                for r in anime:
                    if r[0] == left[0]:
                        right = r

                if right is None:
                    raise RuntimeError(f'Could not find matching anime quality for non-anime quality named: {left[0]}')

                hybrid.append((left[0], min(left[1], right[1]), max(left[2], right[2])))

        guide_definitions['sonarr:hybrid'] = hybrid

    selected_definition = guide_definitions.get(args.type)

    if args.preview:
        utils.quality_preview(selected_definition)
        exit(0)

    print(f'Updating quality definition using {args.type}')
    sonarr = Sonarr(args, logger)
    definition = sonarr.get_quality_definition()
    sonarr.update_quality_definition(definition, selected_definition)

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

def main():
    args = setup_and_parse_args()
    logger = Logger(args)

    load_config(args, logger)

    if args.subcommand == 'profile':
        if args.type.startswith('sonarr:'):
            process_sonarr_profile(args, logger)
        elif args.type.startswith('radarr:'):
            raise NotImplementedError('Radarr guide support is not implemented yet')

    elif args.subcommand == 'quality':
        if args.type.startswith('sonarr:'):
            process_sonarr_quality(args, logger)
        elif args.type.startswith('radarr:'):
            raise NotImplementedError('Radarr quality support is not implemented yet')

if __name__ == '__main__':
    try:
        main()
    except requests.exceptions.HTTPError as e:
        print(e)
        if error_msg := Sonarr.get_error_message(e.response):
            print(f'Response Message: {error_msg}')
        exit(1)
    except Exception as e:
        print(f'ERROR: {e}')
        exit(1)

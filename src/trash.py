import requests

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
            print(f'Updating existing profile: {new_profile_name}')
            sonarr.update_existing_profile(profile_to_update, profile, tag_ids)
        else:
            print(f'Creating new profile: {new_profile_name}')
            sonarr.create_release_profile(new_profile_name, profile, tag_ids)

def process_sonarr_quality(args, logger):
    guide_definitions = quality.parse_markdown(logger, quality.get_markdown())

    if args.type == 'sonarr:hybrid':
        raise ValueError('Hybrid profile not implemented yet')

    selected_definition = guide_definitions.get(args.type)

    if args.preview:
        utils.quality_preview(args.type, selected_definition)
        exit(0)

    print(f'Updating quality definition using {args.type}')
    sonarr = Sonarr(args, logger)
    definition = sonarr.get_quality_definition()
    sonarr.update_quality_definition(definition, selected_definition)

def main():
    args = setup_and_parse_args()
    logger = Logger(args)
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
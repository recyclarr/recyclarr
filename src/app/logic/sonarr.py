import re

from app import guide
from app.guide.profile import types as profile_types
from app.api.sonarr import Sonarr
from app.trash_error import TrashError

# --------------------------------------------------------------------------------------------------
def process_profile(args, logger):
    page = profile_types.get(args.type).get('markdown_doc_name')
    logger.debug(f'Using markdown page: {page}')
    profiles = guide.profile.parse_markdown(args, logger, guide.profile.get_markdown(page))

    # A few false-positive profiles are added sometimes. We filter these out by checking if they
    # actually have meaningful data attached to them, such as preferred terms. If they are mostly empty,
    # we remove them here.
    guide.utils.filter_profiles(profiles)

    if args.preview:
        guide.utils.print_terms_and_scores(profiles)
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
        type_for_name = profile_types.get(args.type).get('profile_typename')
        new_profile_name = f'[Trash] {type_for_name} - {name}'
        profile_to_update = guide.utils.find_existing_profile(new_profile_name, existing_profiles)

        if profile_to_update:
            logger.info(f'Updating existing profile: {new_profile_name}')
            sonarr.update_existing_profile(profile_to_update, profile, tag_ids)
        else:
            logger.info(f'Creating new profile: {new_profile_name}')
            sonarr.create_release_profile(new_profile_name, profile, tag_ids)

# --------------------------------------------------------------------------------------------------
def process_quality(args, logger):
    guide_definitions = guide.quality.parse_markdown(logger, guide.quality.get_markdown())

    if args.type == 'sonarr:hybrid':
        hybrid_quality_regex = re.compile(r'720|1080')
        anime = guide_definitions.get('sonarr:anime')
        nonanime = guide_definitions.get('sonarr:non-anime')
        if len(anime) != len(nonanime):
            raise TrashError('For some reason the anime and non-anime quality definitions are not the same length')

        logger.info(
            'Notice: Hybrid only functions on 720/1080 qualities and uses non-anime values for the rest (e.g. 2160)')

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
                    raise TrashError(f'Could not find matching anime quality for non-anime quality named: {left[0]}')

                hybrid.append((left[0], min(left[1], right[1]), max(left[2], right[2])))

        guide_definitions['sonarr:hybrid'] = hybrid

    selected_definition = guide_definitions.get(args.type)

    if args.preview:
        guide.utils.quality_preview(selected_definition)
        exit(0)

    print(f'Updating quality definition using {args.type}')
    sonarr = Sonarr(args, logger)
    definition = sonarr.get_quality_definition()
    sonarr.update_quality_definition(definition, selected_definition)
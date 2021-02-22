from app.guide.radarr import quality, utils
from app.api.radarr import Radarr
from app.trash_error import TrashError

# --------------------------------------------------------------------------------------------------
def process_quality(args, logger):
    if 0 > args.preferred_percentage > 100:
        raise TrashError(f'Preferred percentage is out of range: {args.preferred_percentage}')

    guide_definitions = quality.parse_markdown(args, logger, quality.get_markdown())
    selected_definition = guide_definitions.get(args.type)

    if args.preview:
        utils.quality_preview(selected_definition)
        exit(0)

    print(f'Updating quality definition using {args.type}')
    server = Radarr(args, logger)
    definition = server.get_quality_definition()
    server.update_quality_definition(definition, selected_definition)
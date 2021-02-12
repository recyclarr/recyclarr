import argparse

# class args: pass
class NoAction(argparse.Action):
    def __init__(self, **kwargs):
        kwargs.setdefault('default', argparse.SUPPRESS)
        kwargs.setdefault('nargs', 0)
        super(NoAction, self).__init__(**kwargs)

    def __call__(self, parser, namespace, values, option_string=None):
        pass

def add_choices_argument(parser, variable_name, help_text, choices: dict):
    parser.register('action', 'none', NoAction)
    parser.add_argument(variable_name, help=help_text, metavar=variable_name.upper(), choices=choices.keys())
    group = parser.add_argument_group(title=f'Choices for {variable_name.upper()}')
    for choice,choice_help in choices.items():
        group.add_argument(choice, help=choice_help, action='none')

def setup_and_parse_args(args_override=None):
    parent_p = argparse.ArgumentParser(add_help=False)
    parent_p.add_argument('--base-uri', help='The base URL for your Sonarr/Radarr instance, for example `http://localhost:8989`. Required if not doing --preview.')
    parent_p.add_argument('--api-key', help='Your API key. Required if not doing --preview.')
    parent_p.add_argument('--preview', help='Only display the processed markdown results and nothing else.',
        action='store_true')
    parent_p.add_argument('--debug', help='Display additional logs useful for development/debug purposes',
        action='store_true')
    parent_p.add_argument('--config-file', help='The configuration YAML file to use. If not specified, the script will look for `trash.yml` in the same directory as the `trash.py` script.')

    parser = argparse.ArgumentParser(description='Automatically mirror TRaSH guides to your Sonarr/Radarr instance.')
    subparsers = parser.add_subparsers(description='Operations specific to different parts of the TRaSH guides', dest='subcommand')

    # Subcommands for 'profile'
    profile_p = subparsers.add_parser('profile', help='Pages of the guide that define profiles',
        parents=[parent_p])
    add_choices_argument(profile_p, 'type', 'The specific guide type/page to pull data from.', {
        'sonarr:anime': 'The anime release profile for Sonarr v3',
        'sonarr:web-dl': 'The WEB-DL release profile for Sonarr v3'
    })
    profile_p.add_argument('--tags', help='Tags to assign to the profiles that are created or updated. These tags will replace any existing tags when updating profiles.',
        nargs='+')

    # Subcommands for 'quality'
    quality_p = subparsers.add_parser('quality', help='Pages in the guide that provide quality definitions',
        parents=[parent_p])
    add_choices_argument(quality_p, 'type', 'The specific guide type/page to pull data from.', {
        'sonarr:anime': 'Choose the Sonarr quality definition best fit for anime',
        'sonarr:non-anime': 'Choose the Sonarr quality definition best fit for tv shows (non-anime)',
        'sonarr:hybrid': 'The script will generate a Sonarr quality definition that works best for all show types'
    })

    return parser.parse_args(args=args_override)

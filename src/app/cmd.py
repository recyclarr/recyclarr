import argparse

# class args: pass

def setup_and_parse_args():
    argparser = argparse.ArgumentParser(description='Automatically mirror TRaSH guides to your *darr instance.')
    argparser.add_argument('--preview', help='Only display the processed markdown results and nothing else.',
        action='store_true')
    argparser.add_argument('--debug', help='Display additional logs useful for development/debug purposes',
        action='store_true')
    argparser.add_argument('--tags', help='Tags to assign to the profiles that are created or updated. These tags will replace any existing tags when updating profiles.',
        nargs='+')
    argparser.add_argument('base_uri', help='The base URL for your Sonarr instance, for example `http://localhost:8989`.')
    argparser.add_argument('api_key', help='Your API key.')
    return argparser.parse_args()#namespace=args)

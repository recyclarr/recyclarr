from discord_webhook import DiscordWebhook, DiscordEmbed
import sys

version = sys.argv[1]
if not version:
    print('Pass version as first argument')
    exit(1)

webhook_url = sys.argv[2]
if not webhook_url:
    print('Pass webhook URL as second argument')
    exit(1)

changelog = sys.argv[3]
if not changelog:
    print('Pass changelog as third argument')
    exit(1)

mkdown_desc = f'''
**Release Notes**
```
{changelog}
```
'''

embed = DiscordEmbed(
    title=f'New Release {version}',
    description=mkdown_desc,
    url=f'https://github.com/recyclarr/recyclarr/releases/tag/{version}'
    )

embed.set_author(
    name='Recyclarr',
    url='https://github.com/recyclarr/recyclarr',
    icon_url='https://github.com/recyclarr/recyclarr/blob/master/ci/notify/trash-icon.png?raw=true')

def add_links(os_name, archs, os):
    url_base = f'https://github.com/recyclarr/recyclarr/releases/download/{version}'
    download_links = ', '.join(f'[{arch}]({url_base}/recyclarr-{os}-{arch}.zip)' for arch in archs)
    embed.add_embed_field(name=os_name, value=f'[{download_links}]')

add_links('Linux', ('x64', 'arm', 'arm64'), 'linux')
add_links('Windows', ('x64', 'arm64'), 'win')
add_links('MacOS', ('x64', 'arm64'), 'osx')

webhook = DiscordWebhook(webhook_url)
webhook.add_embed(embed)
print(webhook.execute())

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
    url=f'https://github.com/rcdailey/trash-updater/releases/tag/{version}'
    )

embed.set_author(
    name='Trash Updater',
    url='https://github.com/rcdailey/trash-updater',
    icon_url='https://github.com/rcdailey/trash-updater/blob/master/ci/notify/trash-icon.png?raw=true')

embed.add_embed_field(name='Linux (x64)',
    value=f'[Download](https://github.com/rcdailey/trash-updater/releases/download/{version}/trash-linux-x64.zip)')

embed.add_embed_field(name='Windows (x64)',
    value=f'[Download](https://github.com/rcdailey/trash-updater/releases/download/{version}/trash-win-x64.zip)')

embed.add_embed_field(name='MacOS (x64)',
    value=f'[Download](https://github.com/rcdailey/trash-updater/releases/download/{version}/trash-osx-x64.zip)')

webhook = DiscordWebhook(webhook_url)
webhook.add_embed(embed)
print(webhook.execute())

from collections import defaultdict
import re
from discord_webhook import DiscordWebhook, DiscordEmbed
import argparse

parser = argparse.ArgumentParser()
parser.add_argument('--repo', required=True)
parser.add_argument('--version', required=True)
parser.add_argument('--webhook-url', required=True)
parser.add_argument('--changelog', required=True)
parser.add_argument('--assets', required=True)
args = parser.parse_args()

with open(args.changelog, 'r') as f:
    changelog_text = f.read()

mkdown_desc = f'''
**Release Notes**
```
{changelog_text}
```
'''

embed = DiscordEmbed(
    title=f'New Release {args.version}',
    description=mkdown_desc,
    url=f'https://github.com/recyclarr/recyclarr/releases/tag/{args.version}'
)

embed.set_author(
    name='Recyclarr',
    url='https://github.com/recyclarr/recyclarr',
    icon_url='https://github.com/recyclarr/recyclarr/blob/master/ci/notify/trash-icon.png?raw=true')

def parse_assets():
    link_groups = defaultdict(list)
    with open(args.assets) as file:
        for line in file.read().splitlines():
            print(f"Processing {line}")
            match = re.search(r"/recyclarr-([\w-]+?)-(\w+)\.", line)
            if match:
                platform = match.group(1)
                arch = match.group(2)
                link_groups[platform].append((arch, line))

    return link_groups

def add_links():
    platform_display_name = {
        'win': 'Windows',
        'osx': 'macOS',
        'linux': 'Linux'
    }

    assets = parse_assets()
    for platform in assets.keys():
        links = ', '.join(f'[{arch}]({link})' for (arch, link) in assets[platform])
        if platform in platform_display_name:
          os_name = platform_display_name[platform]
          embed.add_embed_field(name=os_name, value=f'[{links}]')

add_links()

webhook = DiscordWebhook(args.webhook_url)
webhook.add_embed(embed)
print(webhook.execute())

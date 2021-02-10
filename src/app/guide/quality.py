import requests
import re
from collections import defaultdict

header_regex = re.compile(r'^#+')
table_row_regex = re.compile(r'\| *(.*?) *\| *([\d.]+) *\| *([\d.]+) *\|')

# --------------------------------------------------------------------------------------------------
def get_markdown():
    trash_anime_markdown_url = 'https://raw.githubusercontent.com/TRaSH-/Guides/master/docs/Sonarr/V3/Sonarr-Quality-Settings-File-Size.md'
    response = requests.get(trash_anime_markdown_url)
    return response.content.decode('utf8')

# --------------------------------------------------------------------------------------------------
def parse_markdown(logger, markdown_content):
    results = defaultdict(list)
    table = None

    for line in markdown_content.splitlines():
        if not line:
            continue

        if header_regex.search(line):
            category = 'sonarr:anime' if 'anime' in line.lower() else 'sonarr:non-anime'
            table = results[category]
            if len(table) > 0:
                table = None
        elif (match := table_row_regex.search(line)) and table is not None:
            table.append((match.group(1), float(match.group(2)), float(match.group(3))))

    return results

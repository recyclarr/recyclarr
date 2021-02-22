import requests
import re
from collections import defaultdict

header_regex = re.compile(r'^#+')
table_row_regex = re.compile(r'\| *(.*?) *\| *([\d.]+) *\| *([\d.]+) *\|')

# --------------------------------------------------------------------------------------------------
def get_markdown():
    markdown_page_url = 'https://raw.githubusercontent.com/TRaSH-/Guides/master/docs/Radarr/V3/Radarr-Quality-Settings-File-Size.md'
    response = requests.get(markdown_page_url)
    return response.content.decode('utf8')

# --------------------------------------------------------------------------------------------------
def parse_markdown(args, logger, markdown_content):
    results = defaultdict(list)
    table = None

    # Convert from 0-100 to 0.0-1.0
    preferred_ratio = args.preferred_percentage / 100

    for line in markdown_content.splitlines():
        if not line:
            continue

        if header_regex.search(line):
            category = args.type
            table = results[category]
            if len(table) > 0:
                table = None
        elif (match := table_row_regex.search(line)) and table is not None:
            quality = match.group(1)
            min = float(match.group(2))
            max = float(match.group(3))
            # TODO: Support reading preferred from table data in the guide
            preferred = round(min + (max-min) * preferred_ratio, 1)
            table.append((quality, min, max, preferred))

    return results

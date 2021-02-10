import re
from collections import defaultdict
from enum import Enum
import requests

from ..profile_data import ProfileData

TermCategory = Enum('TermCategory', 'Preferred Required Ignored')

header_regex = re.compile(r'^(#+)\s([\w\s\d]+)\s*$')
score_regex = re.compile(r'score.*?\[(-?[\d]+)\]', re.IGNORECASE)
# included_preferred_regex = re.compile(r'include preferred', re.IGNORECASE)
# not_regex = re.compile(r'not', re.IGNORECASE)
category_regex = (
    (TermCategory.Required, re.compile(r'must contain', re.IGNORECASE)),
    (TermCategory.Ignored, re.compile(r'must not contain', re.IGNORECASE)),
    (TermCategory.Preferred, re.compile(r'preferred', re.IGNORECASE)),
)

# --------------------------------------------------------------------------------------------------
def get_trash_anime_markdown():
    trash_anime_markdown_url = 'https://raw.githubusercontent.com/TRaSH-/Guides/master/docs/Sonarr/V3/Sonarr-Release-Profile-RegEx-Anime.md'
    response = requests.get(trash_anime_markdown_url)
    return response.content.decode('utf8')

# --------------------------------------------------------------------------------------------------
def parse_category(line):
    for rx in category_regex:
        if rx[1].search(line):
            return rx[0]

    return None

# --------------------------------------------------------------------------------------------------
def parse_markdown(logger, markdown_content):
    results = defaultdict(ProfileData)
    profile_name = None
    score = None
    category = None
    bracket_depth = 0

    for line in markdown_content.splitlines():
        # Header processing
        if match := header_regex.search(line):
            header_depth = len(match.group(1))
            header_text = match.group(2)

            # Profile name (always reset previous state here)
            if header_depth == 3:
                score = None
                category = TermCategory.Preferred
                profile_name = header_text
                logger.debug(f'New Profile: {header_text}')
            # Filter type for provided regexes
            elif header_depth == 4:
                category = parse_category(header_text)
                if category:
                    logger.debug(f'  Category Set: {category}')

        # Lines we always look for
        elif line.startswith('```'):
            bracket_depth = 1 - bracket_depth

        # Category-based line processing
        elif profile_name:
            profile = results[profile_name]
            lower_line = line.lower()
            if 'include preferred' in lower_line:
                profile.include_preferred_when_renaming = 'not' not in lower_line
                logger.debug(f'  Include preferred found: {profile.include_preferred_when_renaming}, {lower_line}')
            elif category == TermCategory.Preferred:
                if match := score_regex.search(line):
                    # bracket_depth = 0
                    score = int(match.group(1))
                elif bracket_depth:
                    if score is not None:
                        logger.debug(f'  [Preferred] Score: {score}, Term: {line}')
                        profile.preferred[score].append(line)
            elif category == TermCategory.Ignored and bracket_depth:
                # Sometimes a comma is present at the end of these regexes, because when it's
                # pasted into Sonarr it acts as a delimiter. However, when using them with the
                # API we do not need them.
                profile.ignored.append(line.rstrip(','))
            elif category == TermCategory.Required and bracket_depth:
                profile.required.append(line.rstrip(','))

    logger.debug('\n\n')
    return results

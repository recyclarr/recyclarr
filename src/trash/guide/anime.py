import re
from collections import defaultdict
from enum import Enum
import requests

from ..profile_data import ProfileData

Filter = Enum('FilterType', 'Preferred Required Ignored')

header_regex = re.compile(r'^(#+)\s([\w\s\d]+)\s*$')
score_regex = re.compile(r'score.*?\[(-?[\d]+)\]', re.IGNORECASE)
# included_preferred_regex = re.compile(r'include preferred', re.IGNORECASE)
# not_regex = re.compile(r'not', re.IGNORECASE)
filter_regexes = (
    (Filter.Required, re.compile(r'must contain', re.IGNORECASE)),
    (Filter.Ignored, re.compile(r'must not contain', re.IGNORECASE)),
    (Filter.Preferred, re.compile(r'preferred', re.IGNORECASE)),
)

# --------------------------------------------------------------------------------------------------
def get_trash_anime_markdown():
    trash_anime_markdown_url = 'https://raw.githubusercontent.com/TRaSH-/Guides/master/docs/Sonarr/V3/Sonarr-Release-Profile-RegEx-Anime.md'
    response = requests.get(trash_anime_markdown_url)
    return response.content.decode('utf8')

# --------------------------------------------------------------------------------------------------
def parse_filter(line):
    for rx in filter_regexes:
        if rx[1].search(line):
            return rx[0]

    return None

# --------------------------------------------------------------------------------------------------
def parse_markdown(logger):
    class state:
        results = defaultdict(ProfileData)
        profile_name = None
        score = None
        filter = None
        bracket_depth = 0

    markdown_content = get_trash_anime_markdown()
    for line in markdown_content.splitlines():
        # Header processing
        if match := header_regex.search(line):
            header_depth = len(match.group(1))
            header_text = match.group(2)

            # Profile name (always reset previous state here)
            if header_depth == 3:
                state.score = None
                state.filter = Filter.Preferred
                state.profile_name = header_text
                logger.debug(f'New Profile: {header_text}')
            # Filter type for provided regexes
            elif header_depth == 4:
                state.filter = parse_filter(header_text)
                if state.filter:
                    logger.debug(f'  Filter Set: {state.filter}')

        # Lines we always look for
        elif line.startswith('```'):
            state.bracket_depth = 1 - state.bracket_depth

        # Filter-based line processing
        elif state.profile_name:
            profile = state.results[state.profile_name]
            lower_line = line.lower()
            if 'include preferred' in lower_line:
                profile.include_preferred_when_renaming = 'not' not in lower_line
                logger.debug(f'  Include preferred found: {profile.include_preferred_when_renaming}, {lower_line}')
            elif state.filter == Filter.Preferred:
                if match := score_regex.search(line):
                    # bracket_depth = 0
                    state.score = int(match.group(1))
                elif state.bracket_depth:
                    if state.score is not None:
                        logger.debug(f'  [Preferred] Score: {state.score}, Term: {line}')
                        profile.preferred[state.score].append(line)
            elif state.filter == Filter.Ignored:
                if state.bracket_depth:
                    # Sometimes a comma is present at the end of these regexes, because when it's
                    # pasted into Sonarr it acts as a delimiter. However, when using them with the
                    # API we do not need them.
                    profile.ignored.append(line.rstrip(','))

    logger.debug('\n\n')
    return state.results

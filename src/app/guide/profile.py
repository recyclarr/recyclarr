import re
from collections import defaultdict
from enum import Enum
import requests

from ..profile_data import ProfileData

# This defines general information specific to guide types. Used across different modules as needed.
types = {
    'sonarr:anime': {
        'cmd_help': 'The anime release profile for Sonarr v3',
        'markdown_doc_name': 'Sonarr-Release-Profile-RegEx-Anime',
        'profile_typename': 'Anime'
    },
    'sonarr:web-dl': {
        'cmd_help': 'The WEB-DL release profile for Sonarr v3',
        'markdown_doc_name': 'Sonarr-Release-Profile-RegEx',
        'profile_typename': 'WEB-DL'
    }
}

TermCategory = Enum('TermCategory', 'Preferred Required Ignored')

header_regex = re.compile(r'^(#+)\s([\w\s\d]+)\s*$')
score_regex = re.compile(r'score.*?\[(-?[\d]+)\]', re.IGNORECASE)
header_release_profile_regex = re.compile(r'release profile', re.IGNORECASE)
category_regex = (
    (TermCategory.Required, re.compile(r'must contain', re.IGNORECASE)),
    (TermCategory.Ignored, re.compile(r'must not contain', re.IGNORECASE)),
    (TermCategory.Preferred, re.compile(r'preferred', re.IGNORECASE)),
)

class ParserState:
    def __init__(self):
        self.profile_name = None
        self.score = None
        self.current_category = TermCategory.Preferred
        self.bracket_depth = 0
        self.current_header_depth = -1

    def reset(self):
        self.__init__()

    def is_valid(self):
        return \
            self.profile_name is not None and \
            self.current_category is not None and \
            (self.current_category != TermCategory.Preferred or self.score is not None)

# --------------------------------------------------------------------------------------------------
def get_markdown(page):
    response = requests.get(f'https://raw.githubusercontent.com/TRaSH-/Guides/master/docs/Sonarr/V3/{page}.md')
    return response.content.decode('utf8')

# --------------------------------------------------------------------------------------------------
def parse_category(line):
    for rx in category_regex:
        if rx[1].search(line):
            return rx[0]

    return None

# --------------------------------------------------------------------------------------------------
def parse_markdown_outside_fence(args, logger, line, state, results):
    # Header processing
    if match := header_regex.search(line):
        header_depth = len(match.group(1))
        header_text = match.group(2)
        logger.debug(f'> Parsing Header [Text: {header_text}] [Depth: {header_depth}]')

        # Profile name (always reset previous state here)
        if header_release_profile_regex.search(header_text):
            state.reset()
            state.profile_name = header_text
            logger.debug(f'  - New Profile [Text: {header_text}]')
            return

        elif header_depth <= state.current_header_depth:
            logger.debug('  - !! Non-nested, non-profile header found; resetting all state')
            state.reset()
            return

    # Until we find a header that defines a profile, we don't care about anything under it.
    if not state.profile_name:
        return

    # Check if we are enabling the "Include Preferred when Renaming" checkbox
    profile = results[state.profile_name]
    lower_line = line.lower()
    if 'include preferred' in lower_line:
        profile.include_preferred_when_renaming = 'not' not in lower_line
        logger.debug(f'  - "Include Preferred" found [Value: {profile.include_preferred_when_renaming}] [Line: {line}]')
        return

    # Either we have a nested header or normal line at this point
    # We need to check if we're defining a new category.
    if category := parse_category(line):
        state.current_category = category
        logger.debug(f'  - Category Set [Name: {category}] [Line: {line}]')
        # DO NOT RETURN HERE!
        # The category and score are sometimes in the same sentence (line); continue processing the line!!
        # return

    # Check this line for a score value. We do this even if our category may not be set to 'Preferred' yet.
    if match := score_regex.search(line):
        state.score = int(match.group(1))
        logger.debug(f'  - Score [Value: {state.score}]')
        return

# --------------------------------------------------------------------------------------------------
def parse_markdown_inside_fence(args, logger, line, state, results):
    profile = results[state.profile_name]

    if state.current_category == TermCategory.Preferred:
        logger.debug('    + Capture Term '
                     f'[Category: {state.current_category}] '
                     f'[Score: {state.score}] '
                     f'[Strict: {args.strict_negative_scores}] '
                     f'[Term: {line}]')

        if args.strict_negative_scores and state.score < 0:
            profile.ignored.append(line)
        else:
            profile.preferred[state.score].append(line)
        return

    # Sometimes a comma is present at the end of these regexes, because when it's
    # pasted into Sonarr it acts as a delimiter. However, when using them with the
    # API we do not need them.
    line = line.rstrip(',')

    if state.current_category == TermCategory.Ignored:
        profile.ignored.append(line)
        logger.debug(f'    + Capture Term [Category: {state.current_category}] [Term: {line}]')
        return

    if state.current_category == TermCategory.Required:
        profile.required.append(line)
        logger.debug(f'    + Capture Term [Category: {state.current_category}] [Term: {line}]')
        return

# --------------------------------------------------------------------------------------------------
def parse_markdown(args, logger, markdown_content):
    results = defaultdict(ProfileData)
    state = ParserState()

    for line in markdown_content.splitlines():
        # Always check if we're starting a fenced code block. Whether we are inside one or not greatly affects
        # the logic we use.
        if line.startswith('```'):
            state.bracket_depth = 1 - state.bracket_depth
            continue

        # Not inside brackets
        if state.bracket_depth == 0:
            parse_markdown_outside_fence(args, logger, line, state, results)
        # Inside brackets
        elif state.bracket_depth == 1:
            if not state.is_valid():
                logger.debug('  - !! Inside bracket with invalid state; skipping! '
                             f'[Profile Name: {state.profile_name}] '
                             f'[Category: {state.current_category}] '
                             f'[Score: {state.score}] '
                             f'[Line: {line}] '
                )
            else:
                parse_markdown_inside_fence(args, logger, line, state, results)

    logger.debug('\n')
    return results

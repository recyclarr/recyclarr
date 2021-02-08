from collections import defaultdict

class ProfileData:
    def __init__(self):
        self.preferred = defaultdict(list)
        self.required = []
        self.ignored = []
        # We use 'none' here to represent no explicit mention of the "include preferred" string
        # found in the markdown. We use this to control whether or not the corresponding profile
        # section gets printed in the first place.
        self.include_preferred_when_renaming = None

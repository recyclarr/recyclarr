# --------------------------------------------------------------------------------------------------
# Filter out false-positive profiles that are empty.
def filter_profiles(profiles):
    for name in list(profiles.keys()):
        profile = profiles[name]
        if not len(profile.required) and not len(profile.ignored) and not len(profile.preferred):
            del profiles[name]

# --------------------------------------------------------------------------------------------------
def print_terms_and_scores(profiles):
    for name, profile in profiles.items():
        print(name)

        if profile.include_preferred_when_renaming is not None:
            print('  Include Preferred when Renaming?')
            print('    ' + ('CHECKED' if profile.include_preferred_when_renaming else 'NOT CHECKED'))
            print('')

        if len(profile.required):
            print('  Must Contain:')
            for term in profile.required:
                print(f'    {term}')
            print('')

        if len(profile.ignored):
            print('  Must Not Contain:')
            for term in profile.ignored:
                print(f'    {term}')
            print('')

        if len(profile.preferred):
            print('  Preferred:')
            for score, terms in profile.preferred.items():
                for term in terms:
                    print(f'    {score:<10} {term}')

        print('')

# --------------------------------------------------------------------------------------------------
def find_existing_profile(profile_name, existing_profiles):
    for p in existing_profiles:
        if p.get('name') == profile_name:
            return p
    return None

# --------------------------------------------------------------------------------------------------
def quality_preview(definition):
        print('')
        formats = '{:<20} {:<10} {:<10} {:<10}'
        print(formats.format('Quality', 'Min', 'Max', 'Preferred'))
        print(formats.format('-------', '---', '---', '---'))
        for (quality, min, max, preferred) in definition:
            print(formats.format(quality, min, max, preferred))
        print('')

using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile;

using ProfileDataCollection = IDictionary<string, ProfileData>;

public static class Utils
{
    public static ProfileDataCollection FilterProfiles(ProfileDataCollection profiles, SonarrProfileFilterConfig filter)
    {
        bool IsEmpty(ProfileData data)
        {
            var isEmpty = data is
            {
                // Non-optional
                Required.Count: 0,
                Ignored.Count: 0,
                Preferred.Count: 0
            };

            if (isEmpty && filter.IncludeOptional)
            {
                isEmpty = data is
                {
                    // Optional
                    Optional.Required.Count: 0,
                    Optional.Ignored.Count: 0,
                    Optional.Preferred.Count: 0
                };
            }

            return isEmpty;
        }

        // A few false-positive profiles are added sometimes. We filter these out by checking if they
        // actually have meaningful data attached to them, such as preferred terms. If they are mostly empty,
        // we remove them here.
        return profiles
            .Where(kv => !IsEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public static void PrintTermsAndScores(ProfileDataCollection profiles)
    {
        static void PrintPreferredTerms(string title, IDictionary<int, List<string>> dict)
        {
            if (dict.Count <= 0)
            {
                return;
            }

            Console.WriteLine($"  {title}:");
            foreach (var (score, terms) in dict)
            {
                foreach (var term in terms)
                {
                    Console.WriteLine($"    {score,-10} {term}");
                }
            }

            Console.WriteLine("");
        }

        static void PrintTerms(string title, ICollection<string> terms)
        {
            if (terms.Count == 0)
            {
                return;
            }

            Console.WriteLine($"  {title}:");
            foreach (var term in terms)
            {
                Console.WriteLine($"    {term}");
            }

            Console.WriteLine("");
        }

        Console.WriteLine("");

        foreach (var (name, profile) in profiles)
        {
            Console.WriteLine(name);

            if (profile.IncludePreferredWhenRenaming != null)
            {
                Console.WriteLine("  Include Preferred when Renaming?");
                Console.WriteLine("    " +
                                  (profile.IncludePreferredWhenRenaming.Value ? "CHECKED" : "NOT CHECKED"));
                Console.WriteLine("");
            }

            PrintTerms("Must Contain", profile.Required);
            PrintTerms("Must Contain (Optional)", profile.Optional.Required);
            PrintTerms("Must Not Contain", profile.Ignored);
            PrintTerms("Must Not Contain (Optional)", profile.Optional.Ignored);
            PrintPreferredTerms("Preferred", profile.Preferred);
            PrintPreferredTerms("Preferred (Optional)", profile.Optional.Preferred);

            Console.WriteLine("");
        }
    }
}

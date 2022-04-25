namespace TrashLib.Sonarr.ReleaseProfile;

public static class Utils
{
    public static void PrintTermsAndScores(ReleaseProfileData profile)
    {
        static void PrintPreferredTerms(string title, IReadOnlyCollection<PreferredTermData> preferredTerms)
        {
            if (preferredTerms.Count <= 0)
            {
                return;
            }

            Console.WriteLine($"  {title}:");
            foreach (var (score, terms) in preferredTerms)
            {
                foreach (var term in terms)
                {
                    Console.WriteLine($"    {score,-10} {term}");
                }
            }

            Console.WriteLine("");
        }

        static void PrintTerms(string title, IReadOnlyCollection<TermData> terms)
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

        Console.WriteLine(profile.Name);

        Console.WriteLine("  Include Preferred when Renaming?");
        Console.WriteLine("    " +
                          (profile.IncludePreferredWhenRenaming ? "YES" : "NO"));
        Console.WriteLine("");

        PrintTerms("Must Contain", profile.Required);
        PrintTerms("Must Not Contain", profile.Ignored);
        PrintPreferredTerms("Preferred", profile.Preferred);

        Console.WriteLine("");
    }
}

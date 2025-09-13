using System.Diagnostics.CodeAnalysis;
using Recyclarr.Settings.Models;

namespace Recyclarr.Settings.Deprecations;

public class SettingsDeprecations(IOrderedEnumerable<ISettingsDeprecationCheck> deprecationChecks)
{
    [SuppressMessage(
        "SonarLint",
        "S3267: Loops should be simplified with LINQ expressions",
        Justification = "The 'Where' condition must happen after each Transform() call instead of all at once"
    )]
    public RecyclarrSettings CheckAndTransform(RecyclarrSettings settings)
    {
        foreach (var check in deprecationChecks)
        {
            if (check.CheckIfNeeded(settings))
            {
                settings = check.Transform(settings);
            }
        }

        return settings;
    }
}

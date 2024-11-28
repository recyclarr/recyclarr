using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Config.Parsing.PostProcessing.Deprecations;

public class ConfigDeprecations(IOrderedEnumerable<IConfigDeprecationCheck> deprecationChecks)
{
    [SuppressMessage(
        "SonarLint",
        "S3267: Loops should be simplified with LINQ expressions",
        Justification = "The 'Where' condition must happen after each Transform() call instead of all at once"
    )]
    public T CheckAndTransform<T>(T include)
        where T : ServiceConfigYaml
    {
        foreach (var check in deprecationChecks)
        {
            if (check.CheckIfNeeded(include))
            {
                include = (T)check.Transform(include);
            }
        }

        return include;
    }
}

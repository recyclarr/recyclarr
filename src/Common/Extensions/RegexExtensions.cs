using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Common.Extensions;

public static class RegexExtensions
{
    [SuppressMessage("Design", "CA1021:Avoid out parameters",
        Justification =
            "The out param has a very specific design purpose. It's to allow regex match expressions " +
            "to be executed inside an if condition while also providing match output variable.")]
    public static bool Match(this Regex re, string strToCheck, out Match match)
    {
        match = re.Match(strToCheck);
        return match.Success;
    }
}

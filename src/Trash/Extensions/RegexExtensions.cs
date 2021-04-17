using System.Text.RegularExpressions;

namespace Trash.Extensions
{
    public static class RegexExtensions
    {
        public static bool Match(this Regex re, string strToCheck, out Match match)
        {
            match = re.Match(strToCheck);
            return match.Success;
        }
    }
}

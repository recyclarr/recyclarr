using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrashLib.Sonarr.ReleaseProfile
{
    public interface IReleaseProfileGuideParser
    {
        Task<string> GetMarkdownData(ReleaseProfileType profileName);
        IDictionary<string, ProfileData> ParseMarkdown(ReleaseProfileConfig config, string markdown);
    }
}

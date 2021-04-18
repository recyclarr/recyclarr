using System.Collections.Generic;

namespace Trash.Sonarr.ReleaseProfile
{
    public class ProfileDataOptional
    {
        public List<string> Required { get; init; } = new();
        public List<string> Ignored { get; init; } = new();
        public Dictionary<int, List<string>> Preferred { get; init; } = new();
    }

    public class ProfileData
    {
        public List<string> Required { get; init; } = new();
        public List<string> Ignored { get; init; } = new();
        public Dictionary<int, List<string>> Preferred { get; init; } = new();

        // We use 'null' here to represent no explicit mention of the "include preferred" string
        // found in the markdown. We use this to control whether or not the corresponding profile
        // section gets printed in the first place, or if we modify the existing setting for
        // existing profiles on the server.
        public bool? IncludePreferredWhenRenaming { get; set; }

        public ProfileDataOptional Optional { get; init; } = new();
    }
}

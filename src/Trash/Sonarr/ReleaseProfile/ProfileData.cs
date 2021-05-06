using System.Collections.Generic;

namespace Trash.Sonarr.ReleaseProfile
{
    public class ProfileDataOptional
    {
        public ICollection<string> Required { get; init; } = new List<string>();
        public ICollection<string> Ignored { get; init; } = new List<string>();
        public IDictionary<int, List<string>> Preferred { get; init; } = new Dictionary<int, List<string>>();
    }

    public class ProfileData
    {
        public ICollection<string> Required { get; init; } = new List<string>();
        public ICollection<string> Ignored { get; init; } = new List<string>();
        public IDictionary<int, List<string>> Preferred { get; init; } = new Dictionary<int, List<string>>();

        // We use 'null' here to represent no explicit mention of the "include preferred" string
        // found in the markdown. We use this to control whether or not the corresponding profile
        // section gets printed in the first place, or if we modify the existing setting for
        // existing profiles on the server.
        public bool? IncludePreferredWhenRenaming { get; set; }

        public ProfileDataOptional Optional { get; init; } = new();
    }
}

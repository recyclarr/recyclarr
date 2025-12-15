using Recyclarr.Cli.Console.Helpers;

namespace Recyclarr.Cli.Console.Settings;

internal interface ICacheRebuildSettings
{
    CacheableResourceType? Resource { get; }
    IReadOnlyCollection<string>? Instances { get; }
    bool Preview { get; }
    bool Verbose { get; }
    bool Adopt { get; }
}

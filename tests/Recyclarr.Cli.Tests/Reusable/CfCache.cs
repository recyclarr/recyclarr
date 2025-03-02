using Recyclarr.Cli.Pipelines.CustomFormat.Cache;

namespace Recyclarr.Cli.Tests.Reusable;

internal static class CfCache
{
    public static CustomFormatCache New(params TrashIdMapping[] mappings)
    {
        return new CustomFormatCache(
            new CustomFormatCacheObject { TrashIdMappings = mappings.ToList() }
        );
    }
}

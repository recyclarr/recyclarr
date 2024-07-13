using Recyclarr.Cli.Pipelines.CustomFormat.Cache;

namespace Recyclarr.Cli.TestLibrary;

public static class CfCache
{
    public static CustomFormatCache New(params TrashIdMapping[] mappings)
    {
        return new CustomFormatCache(new CustomFormatCacheObject
        {
            TrashIdMappings = mappings.ToList()
        });
    }
}

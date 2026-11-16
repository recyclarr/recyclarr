using System.IO.Abstractions;
using Recyclarr.ConfigTemplates;
using Recyclarr.ResourceProviders.Storage;

namespace Recyclarr.ResourceProviders.Infrastructure;

/// <summary>
/// Everything derived from provider files at one point in time: guide resource files by resource
/// type, and config template metadata. Built once per refresh and never changed after it is
/// published, so every reader of one snapshot sees one consistent set of guide files.
/// </summary>
public sealed class GuideIndex
{
    public ResourceRegistry<IFileInfo> Files { get; } = new();
    public ResourceRegistry<TemplateMetadata> Templates { get; } = new();

    /// <summary>
    /// Maps each source's current directories in source order, which is precedence order: later
    /// sources override earlier ones.
    /// </summary>
    public static GuideIndex Build(IEnumerable<IResourceSource> sources)
    {
        var index = new GuideIndex();
        foreach (var source in sources)
        {
            foreach (var root in source.Roots)
            {
                source.Strategy.MapResourcePaths(source.Config, root, index);
            }
        }

        return index;
    }
}

using System.IO.Abstractions;
using Recyclarr.ConfigTemplates;

namespace Recyclarr.ResourceProviders.Infrastructure;

/// <summary>
/// The guide index bound to one lifetime scope (an HTTP request or a sync run). Resource queries
/// read guide data only through this, so a scope never mixes data from two refreshes.
/// </summary>
/// <remarks>
/// The index is read when a query runs, not when the scope's services are constructed: FastEndpoints
/// constructs endpoints while mapping routes, before any scope has an index. The owner of the scope
/// binds the index and keeps the underlying provider files from changing until the scope ends.
/// </remarks>
public sealed class ResourceScope
{
    private GuideIndex? _index;

    public static ResourceScope For(GuideIndex index)
    {
        var scope = new ResourceScope();
        scope.Bind(index);
        return scope;
    }

    public void Bind(GuideIndex index)
    {
        if (_index is not null)
        {
            throw new InvalidOperationException("A guide index is already bound to this scope");
        }

        _index = index;
    }

    public GuideIndex Index =>
        _index
        ?? throw new InvalidOperationException(
            "Guide data was read in a scope that holds no resource lease"
        );

    public ResourceRegistry<IFileInfo> Files => Index.Files;
    public ResourceRegistry<TemplateMetadata> Templates => Index.Templates;
}

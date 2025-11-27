namespace Recyclarr.ResourceProviders.Infrastructure;

public class ResourceRegistry<TMetadata>
    where TMetadata : class
{
    private readonly Dictionary<Type, List<TMetadata>> _metadataByResourceType = new();

    public void Register<TResource>(IEnumerable<TMetadata> metadata)
        where TResource : class
    {
        var key = typeof(TResource);
        if (!_metadataByResourceType.TryGetValue(key, out var existingMetadata))
        {
            existingMetadata = [];
            _metadataByResourceType[key] = existingMetadata;
        }

        existingMetadata.AddRange(metadata);
    }

    public IReadOnlyCollection<TMetadata> Get<TResource>()
        where TResource : class
    {
        var key = typeof(TResource);
        return _metadataByResourceType.TryGetValue(key, out var metadata) ? metadata : [];
    }
}

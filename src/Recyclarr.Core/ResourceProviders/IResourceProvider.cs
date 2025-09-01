namespace Recyclarr.ResourceProviders;

public interface IResourceProvider
{
    string Name { get; }
    Task Initialize(CancellationToken token);
}

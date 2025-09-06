namespace Recyclarr.ResourceProviders;

public interface IResourceProvider
{
    string Name { get; }
    string GetSourceDescription();
    Task Initialize(CancellationToken token);
}

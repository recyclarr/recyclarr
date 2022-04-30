namespace Recyclarr.Command;

public interface IServiceCommand
{
    bool Preview { get; }
    bool Debug { get; }
    ICollection<string>? Config { get; }
    string CacheStoragePath { get; }
}

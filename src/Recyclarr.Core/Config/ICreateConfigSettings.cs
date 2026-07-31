namespace Recyclarr.Config;

public interface ICreateConfigSettings
{
    string? Path { get; }
    IReadOnlyCollection<string> Templates { get; }
    bool Force { get; }
}

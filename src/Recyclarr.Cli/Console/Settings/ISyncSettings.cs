using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Console.Settings;

public interface ISyncSettings
{
    SupportedServices? Service { get; }
    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    IReadOnlyCollection<string> Configs { get; }
    bool Preview { get; }
    IReadOnlyCollection<string>? Instances { get; }
}

using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Console.Settings;

internal interface ISyncSettings
{
    SupportedServices? Service { get; }
    IReadOnlyCollection<string> Configs { get; }
    bool Preview { get; }
    IReadOnlyCollection<string>? Instances { get; }
}

using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Console.Settings;

internal interface IListCustomFormatSettings
{
    SupportedServices Service { get; }
    bool ScoreSets { get; }
    bool Raw { get; }
}

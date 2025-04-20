namespace Recyclarr.Cli.Console.Commands;

internal interface IBaseCommandSettings
{
    CancellationToken CancellationToken { get; }
    bool Debug { get; }
    string? AppData { get; }
    bool Raw { get; }
}

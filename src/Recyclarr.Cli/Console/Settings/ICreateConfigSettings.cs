namespace Recyclarr.Cli.Console.Settings;

internal interface ICreateConfigSettings
{
    string? Path { get; }
    IReadOnlyCollection<string> Templates { get; }
    bool Force { get; }
}

namespace Recyclarr.Cli.Console.Settings;

internal interface ICreateConfigSettings
{
    public string? Path { get; }
    public IReadOnlyCollection<string> Templates { get; }
    public bool Force { get; }
}

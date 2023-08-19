namespace Recyclarr.Cli.Console.Settings;

public interface IDeleteCustomFormatSettings
{
    public string InstanceName { get; }
    public IReadOnlyCollection<string> CustomFormatNames { get; }
    public bool All { get; }
    public bool Force { get; }
    bool Preview { get; }
}

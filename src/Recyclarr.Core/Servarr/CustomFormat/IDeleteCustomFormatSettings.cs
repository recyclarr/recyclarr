namespace Recyclarr.Servarr.CustomFormat;

public interface IDeleteCustomFormatSettings
{
    string InstanceName { get; }
    IReadOnlyCollection<string> CustomFormatNames { get; }
    bool All { get; }
    bool Force { get; }
    bool Preview { get; }
}

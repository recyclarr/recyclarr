namespace Recyclarr.Config.Parsing.ErrorHandling;

public interface IConfigDiagnosticCollector
{
    IReadOnlyList<string> Deprecations { get; }
    void AddDeprecation(string message);
}

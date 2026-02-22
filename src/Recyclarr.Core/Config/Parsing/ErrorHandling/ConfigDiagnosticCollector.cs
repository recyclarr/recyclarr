namespace Recyclarr.Config.Parsing.ErrorHandling;

internal class ConfigDiagnosticCollector : IConfigDiagnosticCollector
{
    private readonly List<string> _deprecations = [];

    public IReadOnlyList<string> Deprecations => _deprecations;

    public void AddDeprecation(string message)
    {
        _deprecations.Add(message);
    }
}

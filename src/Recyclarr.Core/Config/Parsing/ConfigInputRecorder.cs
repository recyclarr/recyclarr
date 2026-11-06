namespace Recyclarr.Config.Parsing;

/// <summary>
/// Collects the paths of files a configuration load reads or looks up outside provider data, so a
/// host can detect later edits to them. A path is recorded before it is read, so a missing file is
/// tracked too and creating it counts as a change. One instance per lifetime scope; a scope that
/// loads configuration once gets exactly that load's inputs.
/// </summary>
public sealed class ConfigInputRecorder
{
    private readonly HashSet<string> _paths = [];

    public IReadOnlyCollection<string> Paths => _paths;

    public void Record(string fullPath) => _paths.Add(fullPath);
}

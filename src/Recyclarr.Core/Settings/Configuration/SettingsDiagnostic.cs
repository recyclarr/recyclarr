namespace Recyclarr.Settings.Configuration;

public enum SettingsDiagnosticSeverity
{
    /// <summary>The setting is ignored; settings still load.</summary>
    Warning,

    /// <summary>Settings are invalid; the server must not start.</summary>
    Error,
}

/// <summary>
/// Where a value sits in the YAML file. <see cref="YamlPath"/> uses the file's own key names
/// (<c>resource_providers[0].clone_url</c>); line and column are 1-based.
/// </summary>
public sealed record SettingsPosition(
    string YamlPath,
    string YamlKey,
    SettingsNodeKind Kind,
    int Line,
    int Column
);

public enum SettingsNodeKind
{
    Scalar,
    Mapping,
    Sequence,
}

/// <summary>
/// One problem found while loading or binding <c>settings.yml</c>.
/// </summary>
public sealed record SettingsDiagnostic
{
    public required SettingsDiagnosticSeverity Severity { get; init; }
    public required string File { get; init; }
    public required string Message { get; init; }

    /// <summary>
    /// The location of the problem, or null when it concerns the file as a whole.
    /// </summary>
    public SettingsPosition? Position { get; init; }

    /// <summary>
    /// The message as shown to users: the file, then the line and column when known.
    /// </summary>
    public string Describe()
    {
        return Position is { } p
            ? $"{File} line {p.Line}, column {p.Column}: {Message}"
            : $"{File}: {Message}";
    }
}

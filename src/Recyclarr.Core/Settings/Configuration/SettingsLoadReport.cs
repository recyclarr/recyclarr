namespace Recyclarr.Settings.Configuration;

/// <summary>
/// What the <c>settings.yml</c> configuration provider found: the YAML position of every key it
/// produced and the problems that kept it from loading.
/// </summary>
/// <remarks>
/// Configuration keys alone lose the YAML position and the original key spelling, which
/// user-facing messages and key validation need, so the provider records them here. The provider
/// replaces the contents on each load. Positions are keyed by configuration path, compared
/// without regard to case like configuration keys.
/// </remarks>
public sealed class SettingsLoadReport
{
    private Dictionary<string, SettingsPosition> _positions = new(StringComparer.OrdinalIgnoreCase);
    private List<SettingsDiagnostic> _diagnostics = [];

    /// <summary>
    /// The file name as users see it in messages.
    /// </summary>
    public string FileName { get; internal set; } = "settings.yml";

    public IReadOnlyList<SettingsDiagnostic> Diagnostics => _diagnostics;

    public SettingsPosition? Find(string configurationPath)
    {
        return _positions.GetValueOrDefault(configurationPath);
    }

    internal void Replace(
        Dictionary<string, SettingsPosition> positions,
        List<SettingsDiagnostic> diagnostics
    )
    {
        _positions = positions;
        _diagnostics = diagnostics;
    }

    internal SettingsDiagnostic Error(string message, SettingsPosition? position = null)
    {
        return new SettingsDiagnostic
        {
            Severity = SettingsDiagnosticSeverity.Error,
            File = FileName,
            Message = message,
            Position = position,
        };
    }
}

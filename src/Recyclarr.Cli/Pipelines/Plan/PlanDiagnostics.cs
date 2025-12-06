namespace Recyclarr.Cli.Pipelines.Plan;

internal class PlanDiagnostics(ILogger log)
{
    private readonly List<string> _invalidTrashIds = [];
    private readonly List<InvalidNamingEntry> _invalidNamingFormats = [];
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];

    public IReadOnlyCollection<string> InvalidTrashIds => _invalidTrashIds;
    public IReadOnlyCollection<InvalidNamingEntry> InvalidNamingFormats => _invalidNamingFormats;
    public IReadOnlyCollection<string> Warnings => _warnings;
    public IReadOnlyCollection<string> Errors => _errors;

    public bool ShouldProceed => _errors.Count == 0;

    public void AddInvalidTrashId(string trashId)
    {
        log.Debug("Invalid trash_id: {TrashId}", trashId);
        _invalidTrashIds.Add(trashId);
    }

    public void AddInvalidNaming(string type, string configValue)
    {
        log.Debug("Invalid {Type} naming format: {ConfigValue}", type, configValue);
        _invalidNamingFormats.Add(new InvalidNamingEntry(type, configValue));
    }

    public void AddWarning(string message)
    {
        log.Debug("Plan warning: {Message}", message);
        _warnings.Add(message);
    }

    public void AddError(string message)
    {
        log.Debug("Plan error: {Message}", message);
        _errors.Add(message);
    }
}

internal record InvalidNamingEntry(string Type, string ConfigValue);

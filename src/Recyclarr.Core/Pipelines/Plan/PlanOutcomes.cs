using Recyclarr.Sync;

namespace Recyclarr.Pipelines.Plan;

public record InvalidNamingFormatOutcome(string FormatType, string ConfigValue) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Error;
    public override SyncOutcomeScope Scope => SyncOutcomeScope.ResourceLocal;
}

public record QualityDefinitionNotFoundOutcome(string Type) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Error;
    public override SyncOutcomeScope Scope => SyncOutcomeScope.ResourceLocal;
}

public record QualityNotFoundOutcome(string Quality, string Type) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Error;
    public override SyncOutcomeScope Scope => SyncOutcomeScope.ResourceLocal;
}

public record PreferredRatioClampedOutcome(decimal Original, decimal Clamped) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Warning;
}

public record MinGreaterThanPreferredOutcome(string Quality, decimal Min, decimal Preferred)
    : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Error;
    public override SyncOutcomeScope Scope => SyncOutcomeScope.ResourceLocal;
}

public record UnlimitedPreferredGreaterThanMaxOutcome(string Quality, decimal Max) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Error;
    public override SyncOutcomeScope Scope => SyncOutcomeScope.ResourceLocal;
}

public record PreferredGreaterThanMaxOutcome(string Quality, decimal Preferred, decimal Max)
    : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Error;
    public override SyncOutcomeScope Scope => SyncOutcomeScope.ResourceLocal;
}

public record DuplicateQualityProfileNameOutcome(string Name) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Error;
    public override SyncOutcomeScope Scope => SyncOutcomeScope.ResourceLocal;
}

public record CustomFormatServiceIdCollisionOutcome(
    string ExistingName,
    string ExistingTrashId,
    string NewName,
    string NewTrashId,
    int ServiceId
) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Error;
    public override SyncOutcomeScope Scope => SyncOutcomeScope.ResourceLocal;
}

public record InvalidQualityProfileTrashIdOutcome(string TrashId) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Warning;
}

public record InvalidCustomFormatTrashIdOutcome(string TrashId) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Warning;
}

public record InvalidCfGroupSkipIdOutcome(string TrashId) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Warning;
}

public record IncompatibleCfGroupOutcome(string Name, string TrashId) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Warning;
}

public record EmptyCfGroupOutcome(string Name, string TrashId) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Warning;
}

public record AmbiguousProfileReferenceOutcome(
    string Context,
    string TrashId,
    IReadOnlyList<string> ProfileNames
) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => SyncDiagnosticLevel.Error;
}

public record RuleValidationOutcome(
    SyncDiagnosticLevel Severity,
    string PropertyName,
    string Message,
    string? AttemptedValue,
    string? ErrorCode
) : SyncOutcome
{
    public override SyncDiagnosticLevel Level => Severity;
}

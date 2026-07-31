using Recyclarr.Sync;

namespace Recyclarr.Pipelines.Plan;

public record InvalidNamingFormatOutcome(string FormatType, string ConfigValue)
    : SyncOutcome(SyncDiagnosticLevel.Error);

public record QualityDefinitionNotFoundOutcome(string Type)
    : SyncOutcome(SyncDiagnosticLevel.Error);

public record QualityNotFoundOutcome(string Quality, string Type)
    : SyncOutcome(SyncDiagnosticLevel.Error);

public record PreferredRatioClampedOutcome(decimal Original, decimal Clamped)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record MinGreaterThanPreferredOutcome(string Quality, decimal Min, decimal Preferred)
    : SyncOutcome(SyncDiagnosticLevel.Error);

public record UnlimitedPreferredGreaterThanMaxOutcome(string Quality, decimal Max)
    : SyncOutcome(SyncDiagnosticLevel.Error);

public record PreferredGreaterThanMaxOutcome(string Quality, decimal Preferred, decimal Max)
    : SyncOutcome(SyncDiagnosticLevel.Error);

public record DuplicateQualityProfileNameOutcome(string Name)
    : SyncOutcome(SyncDiagnosticLevel.Error);

public record CustomFormatServiceIdCollisionOutcome(
    string ExistingName,
    string ExistingTrashId,
    string NewName,
    string NewTrashId,
    int ServiceId
) : SyncOutcome(SyncDiagnosticLevel.Error);

public record InvalidQualityProfileTrashIdOutcome(string TrashId)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record InvalidCustomFormatTrashIdOutcome(string TrashId)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record InvalidCfGroupSkipIdOutcome(string TrashId)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record IncompatibleCfGroupOutcome(string Name, string TrashId)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record EmptyCfGroupOutcome(string Name, string TrashId)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record AmbiguousProfileReferenceOutcome(
    string Context,
    string TrashId,
    IReadOnlyList<string> ProfileNames
) : SyncOutcome(SyncDiagnosticLevel.Error);

public record RuleValidationOutcome(
    SyncDiagnosticLevel Severity,
    string PropertyName,
    string Message,
    string? AttemptedValue,
    string? ErrorCode
) : SyncOutcome(Severity);

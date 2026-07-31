using Recyclarr.Sync;

namespace Recyclarr.Pipelines.QualityProfile;

public record NonExistentQualityProfilesOutcome(IReadOnlyList<string> Names)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record InvalidQualityProfileOutcome(
    string ProfileName,
    string PropertyName,
    string Message,
    string? AttemptedValue,
    string? ErrorCode
) : SyncOutcome(SyncDiagnosticLevel.Error);

public record InvalidQualityNamesOutcome(string ProfileName, IReadOnlyList<string> Names)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record InvalidExceptCustomFormatNamesOutcome(string ProfileName, IReadOnlyList<string> Names)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record UnmatchedExceptCustomFormatPatternsOutcome(
    string ProfileName,
    IReadOnlyList<string> Patterns
) : SyncOutcome(SyncDiagnosticLevel.Warning);

public record ReplacedQualityProfilesOutcome(IReadOnlyList<string> Names)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

public record QualityProfileRenameConflictOutcome(string Name)
    : SyncOutcome(SyncDiagnosticLevel.Error);

public record AmbiguousQualityProfileOutcome(
    string ProfileName,
    IReadOnlyList<(string Name, int Id)> ServiceMatches
) : SyncOutcome(SyncDiagnosticLevel.Error);

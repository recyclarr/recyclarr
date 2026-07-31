using Recyclarr.Sync;

namespace Recyclarr.Pipelines.CustomFormat;

public record AmbiguousCustomFormatOutcome(
    string GuideName,
    IReadOnlyList<(string Name, int Id)> ServiceMatches
) : SyncOutcome(SyncDiagnosticLevel.Error);

public record ReplacedCustomFormatsOutcome(IReadOnlyList<string> Names)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

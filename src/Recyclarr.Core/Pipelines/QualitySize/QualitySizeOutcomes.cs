using Recyclarr.Sync;

namespace Recyclarr.Pipelines.QualitySize;

public record MissingServerQualityDefinitionOutcome(string Quality)
    : SyncOutcome(SyncDiagnosticLevel.Warning);

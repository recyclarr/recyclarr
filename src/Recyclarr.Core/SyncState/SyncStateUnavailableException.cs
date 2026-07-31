using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.SyncState;

[SuppressMessage(
    "Design",
    "CA1064:Exceptions should be public",
    Justification = "This exception only classifies failures within Core orchestration."
)]
internal sealed class SyncStateUnavailableException(Exception innerException)
    : Exception("Sync state storage is unavailable.", innerException);

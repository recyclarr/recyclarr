namespace Recyclarr.Sync;

/// <summary>
/// Records unexpected sync exceptions behind the opaque reference exposed to result consumers.
/// </summary>
/// <remarks>
/// Implementations receive exception details at this trusted boundary. Terminal result contracts
/// expose only the corresponding reference.
/// </remarks>
public interface ISyncFaultReporter
{
    /// <summary>
    /// Records an unexpected exception under its externally shareable reference.
    /// </summary>
    void Report(string reference, Exception exception);
}

namespace Recyclarr.Compatibility;

/// <summary>
/// The version and API target of the service an instance scope talks to, established once by
/// <see cref="ServiceAgnosticCapabilityEnforcer"/> after the service passed compatibility checks.
/// </summary>
/// <remarks>
/// Registered only in the child scope that a processor begins after detection. Components that
/// depend on it (version-specific gateways, capability-aware planning) therefore cannot be
/// resolved before detection succeeds; attempting it fails resolution instead of guessing.
/// </remarks>
public record DetectedService(Version Version, ServiceApiTarget ApiTarget);

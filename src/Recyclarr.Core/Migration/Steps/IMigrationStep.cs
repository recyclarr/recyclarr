namespace Recyclarr.Migration.Steps;

/// <summary>
/// Converts on-disk data left by an older Recyclarr version to the current layout. Every step runs
/// at each server startup, so <see cref="Run"/> must do nothing when the old layout is absent and
/// log only the changes it makes.
/// </summary>
internal interface IMigrationStep
{
    /// <summary>
    /// What the step does, shown to the user when it fails.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Distinct manual fixes the user can try when the step fails.
    /// </summary>
    IReadOnlyCollection<string> Remediation { get; }

    void Run(ILogger log);
}

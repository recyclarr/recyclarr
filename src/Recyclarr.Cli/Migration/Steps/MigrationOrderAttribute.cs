namespace Recyclarr.Cli.Migration.Steps;

/// <summary>
/// Specifies the execution order for a migration step. Lower values run first.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class MigrationOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}

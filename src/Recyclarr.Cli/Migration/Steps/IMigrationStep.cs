using Spectre.Console;

namespace Recyclarr.Cli.Migration.Steps;

public interface IMigrationStep
{
    /// <summary>
    /// Determines the order in which this migration step will run.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// A description printed to the user so that they understand the purpose of this migration step, and
    /// what it does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// One or more strings that individually represent a distinct manual solution that a user can attempt
    /// when this migration step fails.
    /// </summary>
    IReadOnlyCollection<string> Remediation { get; }

    /// <summary>
    /// Determines if this migration step is required. If so, it must be executed before the program may be
    /// used normally.
    /// </summary>
    bool Required { get; }

    /// <summary>
    /// Run logic to determine if this migration step is necessary
    /// </summary>
    /// <returns>
    /// Return true if this migration step is needed and should be run. False means the migration step is
    /// not needed and Execute() will not be called.
    /// </returns>
    bool CheckIfNeeded();

    /// <summary>
    /// Execute the logic necessary for this migration step.
    /// </summary>
    /// <param name="console">
    /// Use the console to print additional debug diagnostics. If this parameter is null, that means those
    /// diagnostics should not be printed.
    /// </param>
    void Execute(IAnsiConsole? console);
}

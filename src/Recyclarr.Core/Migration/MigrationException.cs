namespace Recyclarr.Migration;

/// <summary>
/// A migration step failed. The message names the step, the cause, and the manual fixes, because
/// it reaches the user only through the server's fatal startup log.
/// </summary>
public class MigrationException(
    string description,
    IReadOnlyCollection<string> remediation,
    Exception innerException
) : FatalException(BuildMessage(description, remediation, innerException), innerException)
{
    private static string BuildMessage(
        string description,
        IReadOnlyCollection<string> remediation,
        Exception innerException
    )
    {
        var fixes = string.Concat(remediation.Select(x => $"{Environment.NewLine}- {x}"));
        return $"Migration step failed: {description}: {innerException.Message}"
            + $"{Environment.NewLine}Possible fixes:{fixes}";
    }
}

namespace Recyclarr.Cli.Migration;

public class MigrationException(
    Exception originalException,
    string operationDescription,
    IReadOnlyCollection<string> remediation)
    : Exception
{
    public Exception OriginalException { get; } = originalException;
    public string OperationDescription { get; } = operationDescription;
    public IReadOnlyCollection<string> Remediation { get; } = remediation;
}

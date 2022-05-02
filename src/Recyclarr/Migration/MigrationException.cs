namespace Recyclarr.Migration;

public class MigrationException : Exception
{
    public MigrationException(
        Exception originalException,
        string operationDescription,
        IReadOnlyCollection<string> remediation)
    {
        OperationDescription = operationDescription;
        OriginalException = originalException;
        Remediation = remediation;
    }

    public Exception OriginalException { get; }
    public string OperationDescription { get; }
    public IReadOnlyCollection<string> Remediation { get; }
}

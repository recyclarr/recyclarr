namespace Recyclarr.Migration;

public class MigrationException : Exception
{
    public MigrationException(string operationDescription, string failureReason)
    {
        OperationDescription = operationDescription;
        FailureReason = failureReason;
    }

    public string OperationDescription { get; }
    public string FailureReason { get; }

    public override string Message =>
        $"Fatal exception during migration step [Desc: {OperationDescription}] [Reason: {FailureReason}]";
}

using Recyclarr.Sync;

namespace Recyclarr.ErrorHandling;

public abstract record HandledInstanceFailure : SyncOutcome
{
    protected HandledInstanceFailure()
        : base(SyncDiagnosticLevel.Error) { }
}

public record NoConfigurationFilesFailure : HandledInstanceFailure;

public record InvalidInstancesFailure(IReadOnlyList<string> InstanceNames) : HandledInstanceFailure;

public record DuplicateInstancesFailure(IReadOnlyList<string> InstanceNames)
    : HandledInstanceFailure;

public record SplitInstancesFailure(IReadOnlyList<string> InstanceNames) : HandledInstanceFailure;

public record InvalidConfigurationFilesFailure(IReadOnlyList<string> FileNames)
    : HandledInstanceFailure;

public record InvalidConfigurationFailure : HandledInstanceFailure;

public record PostProcessingFailure(string Message) : HandledInstanceFailure;

public record EnvironmentFailure(string Message) : HandledInstanceFailure;

public record ServiceFailure(string Message) : HandledInstanceFailure;

public record GitFailure(int ExitCode) : HandledInstanceFailure;

public record HttpConnectionFailure : HandledInstanceFailure;

public record HttpApiFailure(
    int StatusCode,
    IReadOnlyList<HttpApiFailureMessage> ResponseMessages,
    bool HasRequestContent,
    string? RequestBody
) : HandledInstanceFailure;

public abstract record HttpApiFailureMessage(string Message);

public record HttpApiResponseMessage(string Message) : HttpApiFailureMessage(Message);

public record HttpApiFieldError(string Field, string Message) : HttpApiFailureMessage(Message);

public record MigrationFailure(
    string OperationDescription,
    string Reason,
    IReadOnlyList<string> Remediation
) : HandledInstanceFailure;

public record ContextualValidationFailure(
    string Context,
    string? ErrorPrefix,
    IReadOnlyList<ValidationFailureDetail> Failures
) : HandledInstanceFailure;

public record ValidationFailureDetail(
    string PropertyName,
    string Message,
    string? AttemptedValue,
    string? ErrorCode
);

public record ConfigParsingFailure(string? FileName, int Line, string Message)
    : HandledInstanceFailure;

public record YamlErrorFailure(int Line, string Message) : HandledInstanceFailure;

public record YamlParseFailure(int Line) : HandledInstanceFailure;

using FluentValidation;

namespace Recyclarr.Common.FluentValidation;

public class ContextualValidationException(
    ValidationException originalException,
    string errorPrefix,
    string validationContext
) : Exception
{
    public ValidationException OriginalException { get; } = originalException;
    public string ErrorPrefix { get; } = errorPrefix;
    public string ValidationContext { get; } = validationContext;

    public void LogErrors(ValidationLogger logger)
    {
        logger.LogValidationErrors(OriginalException.Errors, ErrorPrefix);
        logger.LogTotalErrorCount(ValidationContext);
    }
}

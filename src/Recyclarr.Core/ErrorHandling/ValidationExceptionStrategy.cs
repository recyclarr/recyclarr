using Recyclarr.Common.FluentValidation;

namespace Recyclarr.ErrorHandling;

internal class ValidationExceptionStrategy : IExceptionStrategy
{
    public Task<HandledInstanceFailure?> HandleAsync(Exception exception)
    {
        if (exception is not ContextualValidationException e)
        {
            return Task.FromResult<HandledInstanceFailure?>(null);
        }

        var failures = e
            .OriginalException.Errors.Select(error => new ValidationFailureDetail(
                error.PropertyName,
                error.ErrorMessage,
                error.AttemptedValue?.ToString(),
                error.ErrorCode
            ))
            .ToList();

        return Task.FromResult<HandledInstanceFailure?>(
            new ContextualValidationFailure(e.ValidationContext, e.ErrorPrefix, failures)
        );
    }
}

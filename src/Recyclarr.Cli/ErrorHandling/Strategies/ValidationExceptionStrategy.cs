using Recyclarr.Common.FluentValidation;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class ValidationExceptionStrategy : IExceptionStrategy
{
    public Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        if (exception is not ContextualValidationException e)
        {
            return Task.FromResult<IReadOnlyList<string>?>(null);
        }

        var messages = e
            .OriginalException.Errors.Select(err =>
                !string.IsNullOrEmpty(e.ErrorPrefix)
                    ? $"[{e.ErrorPrefix}] {err.ErrorMessage}"
                    : err.ErrorMessage
            )
            .ToList();

        messages.Add($"{e.ValidationContext} failed with {messages.Count} error(s)");

        return Task.FromResult<IReadOnlyList<string>?>(messages);
    }
}

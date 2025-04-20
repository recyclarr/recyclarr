using System.Diagnostics.CodeAnalysis;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Cli.Processors.ErrorHandling;

internal class FlurlHttpExceptionHandler(ILogger log)
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task ProcessServiceErrorMessages(IServiceErrorMessageExtractor extractor)
    {
        if (extractor.ExceptionMessage.Contains("task was canceled", StringComparison.Ordinal))
        {
            log.Error("Reason: User canceled the operation");
            return;
        }

        switch (extractor.HttpStatusCode)
        {
            case 401:
                log.Error(
                    "Reason: Recyclarr is unauthorized to talk to the service. Is your `api_key` correct?"
                );
                return;

            case null:
                log.Error("Reason: Problem connecting to the service. Is your `base_url` correct?");
                return;
        }

        ProcessBody(await extractor.GetErrorMessage());
    }

    private void ProcessBody(string responseBody)
    {
        var parser = new ErrorResponseParser(log, responseBody);

        // Try to parse validation errors
        if (
            parser.DeserializeList(s =>
                s.Select(x => x.GetProperty("errorMessage").GetString())
                    .NotNull(x => !string.IsNullOrEmpty(x))
            )
        )
        {
            return;
        }

        // Try to parse single error message
        if (parser.Deserialize(s => s.GetProperty("message").GetString()))
        {
            return;
        }

        // A list of errors with a title
        if (parser.DeserializeServiceErrorList())
        {
            return;
        }

        // Last resort
        log.Error(
            "Reason: Unable to determine. Please report this as a bug and attach your `debug.log` and `verbose.log` files"
        );
    }
}

using System.Diagnostics.CodeAnalysis;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Cli.Processors.ErrorHandling;

public class FlurlHttpExceptionHandler : IFlurlHttpExceptionHandler
{
    private readonly ILogger _log;

    public FlurlHttpExceptionHandler(ILogger log)
    {
        _log = log;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task ProcessServiceErrorMessages(IServiceErrorMessageExtractor extractor)
    {
        var statusCode = extractor.GetHttpStatusCode();

        switch (statusCode)
        {
            case 401:
                _log.Error("Reason: Recyclarr is unauthorized to talk to the service. Is your `api_key` correct?");
                break;

            default:
                ProcessBody(await extractor.GetErrorMessage());
                break;
        }
    }

    private void ProcessBody(string responseBody)
    {
        var parser = new ErrorResponseParser(_log, responseBody);

        if (parser.DeserializeList(s => s
            .Select(x => (string) x.errorMessage)
            .NotNull(x => !string.IsNullOrEmpty(x))))
        {
            return;
        }

        if (parser.Deserialize(s => s.message))
        {
            return;
        }

        // Last resort
        _log.Error("Reason: Unable to determine. Please report this as a bug and attach your `verbose.log` file.");
    }
}

using Flurl.Http;

namespace Recyclarr.Cli.Processors.ErrorHandling;

public class ServiceErrorMessageExtractor(FlurlHttpException e) : IServiceErrorMessageExtractor
{
    public async Task<string> GetErrorMessage()
    {
        return await e.GetResponseStringAsync();
    }

    public int? GetHttpStatusCode()
    {
        return e.StatusCode;
    }
}

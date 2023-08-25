using Flurl.Http;

namespace Recyclarr.Cli.Processors.ErrorHandling;

public class ServiceErrorMessageExtractor : IServiceErrorMessageExtractor
{
    private readonly FlurlHttpException _e;

    public ServiceErrorMessageExtractor(FlurlHttpException e)
    {
        _e = e;
    }

    public async Task<string> GetErrorMessage()
    {
        return await _e.GetResponseStringAsync();
    }
}

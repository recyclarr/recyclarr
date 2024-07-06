using Flurl.Http;

namespace Recyclarr.Cli.Processors.ErrorHandling;

public class ServiceErrorMessageExtractor(FlurlHttpException e) : IServiceErrorMessageExtractor
{
    public HttpRequestError? HttpError
    {
        get
        {
            if (e.InnerException is not HttpRequestException http)
            {
                return null;
            }

            return http.HttpRequestError;
        }
    }

    public async Task<string> GetErrorMessage()
    {
        return await e.GetResponseStringAsync();
    }

    public int? HttpStatusCode => e.StatusCode;

    public string ExceptionMessage => e.Message;
}

namespace Recyclarr.Cli.Processors.ErrorHandling;

internal interface IServiceErrorMessageExtractor
{
    Task<string> GetErrorMessage();
    int? HttpStatusCode { get; }
    HttpRequestError? HttpError { get; }
    string ExceptionMessage { get; }
}

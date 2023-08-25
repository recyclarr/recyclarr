namespace Recyclarr.Cli.Processors.ErrorHandling;

public interface IServiceErrorMessageExtractor
{
    Task<string> GetErrorMessage();
}

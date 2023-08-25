namespace Recyclarr.Cli.Processors.ErrorHandling;

public interface IFlurlHttpExceptionHandler
{
    Task ProcessServiceErrorMessages(IServiceErrorMessageExtractor extractor);
}

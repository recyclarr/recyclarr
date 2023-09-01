using Flurl.Http;
using Recyclarr.TrashLib.Config.Parsing.ErrorHandling;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Repo.VersionControl;

namespace Recyclarr.Cli.Processors.ErrorHandling;

public class ConsoleExceptionHandler
{
    private readonly ILogger _log;
    private readonly IFlurlHttpExceptionHandler _httpExceptionHandler;

    public ConsoleExceptionHandler(ILogger log, IFlurlHttpExceptionHandler httpExceptionHandler)
    {
        _log = log;
        _httpExceptionHandler = httpExceptionHandler;
    }

    public async Task HandleException(Exception sourceException)
    {
        switch (sourceException)
        {
            case GitCmdException e:
                _log.Error(e, "Non-zero exit code {ExitCode} while executing Git command: {Error}",
                    e.ExitCode, e.Error);
                break;

            case FlurlHttpException e:
                _log.Error("HTTP error: {Message}", e.SanitizedExceptionMessage());
                await _httpExceptionHandler.ProcessServiceErrorMessages(new ServiceErrorMessageExtractor(e));
                break;

            case NoConfigurationFilesException:
                _log.Error("No configuration files found");
                break;

            case InvalidInstancesException e:
                _log.Error("The following instances do not exist: {Names}", e.InstanceNames);
                break;

            case DuplicateInstancesException e:
                _log.Error("The following instance names are duplicated: {Names}", e.InstanceNames);
                _log.Error("Instance names are unique and may not be reused");
                break;

            case SplitInstancesException e:
                _log.Error("The following configs share the same `base_url`, which isn't allowed: {Instances}",
                    e.InstanceNames);
                _log.Error(
                    "Consolidate the config files manually to fix. " +
                    "See: https://recyclarr.dev/wiki/yaml/config-examples/#merge-single-instance");
                break;

            case InvalidConfigurationFilesException e:
                _log.Error("Manually-specified configuration files do not exist: {Files}", e.InvalidFiles);
                break;

            case PostProcessingException e:
                _log.Error("Configuration post-processing failed: {Message}", e.Message);
                break;

            case CommandException e:
                _log.Error(e.Message);
                break;

            // This handles non-deterministic/unexpected exceptions.
            default:
                throw sourceException;
        }
    }
}

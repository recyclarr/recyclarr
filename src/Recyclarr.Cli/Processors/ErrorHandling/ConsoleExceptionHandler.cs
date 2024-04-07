using Flurl.Http;
using Recyclarr.Cli.Console;
using Recyclarr.Compatibility;
using Recyclarr.Config.ExceptionTypes;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Http;
using Recyclarr.VersionControl;

namespace Recyclarr.Cli.Processors.ErrorHandling;

public class ConsoleExceptionHandler(ILogger log, IFlurlHttpExceptionHandler httpExceptionHandler)
{
    public async Task<bool> HandleException(Exception sourceException)
    {
        switch (sourceException)
        {
            case GitCmdException e:
                log.Error(e, "Non-zero exit code {ExitCode} while executing Git command: {Error}",
                    e.ExitCode, e.Error);
                break;

            case FlurlHttpException e:
                log.Error("HTTP error: {Message}", e.SanitizedExceptionMessage());
                await httpExceptionHandler.ProcessServiceErrorMessages(new ServiceErrorMessageExtractor(e));
                break;

            case NoConfigurationFilesException:
                log.Error("No configuration files found");
                break;

            case InvalidInstancesException e:
                log.Error("The following instances do not exist: {Names}", e.InstanceNames);
                break;

            case DuplicateInstancesException e:
                log.Error("The following instance names are duplicated: {Names}", e.InstanceNames);
                log.Error("Instance names are unique and may not be reused");
                break;

            case SplitInstancesException e:
                log.Error("The following configs share the same `base_url`, which isn't allowed: {Instances}",
                    e.InstanceNames);
                log.Error(
                    "Consolidate the config files manually to fix. " +
                    "See: https://recyclarr.dev/wiki/yaml/config-examples/#merge-single-instance");
                break;

            case InvalidConfigurationFilesException e:
                log.Error("Manually-specified configuration files do not exist: {Files}", e.InvalidFiles);
                break;

            case PostProcessingException e:
                log.Error("Configuration post-processing failed: {Message}", e.Message);
                break;

            case ServiceIncompatibilityException e:
                log.Error(e.Message);
                break;

            case CommandException e:
                log.Error(e.Message);
                break;

            default:
                return false;
        }

        return true;
    }
}

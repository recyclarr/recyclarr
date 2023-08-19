using Flurl.Http;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Parsing.ErrorHandling;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Repo.VersionControl;

namespace Recyclarr.Cli.Processors;

public class ConsoleExceptionHandler
{
    private readonly ILogger _log;

    public ConsoleExceptionHandler(ILogger log)
    {
        _log = log;
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
                foreach (var error in await GetValidationErrorsAsync(e))
                {
                    _log.Error("Reason: {Error}", error);
                }

                break;

            case NoConfigurationFilesException:
                _log.Error("No configuration files found");
                break;

            case InvalidInstancesException e:
                _log.Error("The following instances do not exist: {Names}", e.InstanceNames);
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

            case CommandException e:
                _log.Error(e.Message);
                break;

            // This handles non-deterministic/unexpected exceptions.
            default:
                throw sourceException;
        }
    }

    private static async Task<IReadOnlyCollection<string>> GetValidationErrorsAsync(FlurlHttpException e)
    {
        var response = await e.GetResponseJsonAsync<List<dynamic>>();
        if (response is null)
        {
            return Array.Empty<string>();
        }

        return response
            .Select(x => (string) x.errorMessage)
            .NotNull(x => !string.IsNullOrEmpty(x))
            .ToList();
    }
}

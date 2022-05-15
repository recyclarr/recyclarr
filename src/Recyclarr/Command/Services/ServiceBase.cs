using CliFx.Exceptions;
using Flurl.Http;
using Recyclarr.Command.Helpers;
using Serilog;
using TrashLib.Extensions;
using YamlDotNet.Core;

namespace Recyclarr.Command.Services;

/// <summary>
///     Mainly intended to handle common exception recovery logic between service command classes.
/// </summary>
public abstract class ServiceBase<T> where T : IServiceCommand
{
    private readonly ILogger _log;
    private readonly IServiceInitialization _serviceInitialization;

    protected ServiceBase(ILogger log, IServiceInitialization serviceInitialization)
    {
        _log = log;
        _serviceInitialization = serviceInitialization;
    }

    public async Task Execute(T cmd)
    {
        try
        {
            _serviceInitialization.Initialize(cmd);
            await Process(cmd);
        }
        catch (YamlException e)
        {
            var message = e.InnerException is not null ? e.InnerException.Message : e.Message;
            _log.Error("Found Unrecognized YAML Property: {ErrorMsg}", message);
            _log.Error("Please remove the property quoted in the above message from your YAML file");
            throw new CommandException("Exiting due to invalid configuration");
        }
        catch (FlurlHttpException e)
        {
            _log.Error("HTTP error while communicating with {ServiceName}: {Msg}", cmd.Name,
                e.SanitizedExceptionMessage());
            ExitDueToFailure();
        }
        catch (Exception e) when (e is not CommandException)
        {
            _log.Error(e, "Unrecoverable Exception");
            ExitDueToFailure();
        }
    }

    protected abstract Task Process(T cmd);

    private static void ExitDueToFailure()
        => throw new CommandException("Exiting due to previous exception");
}

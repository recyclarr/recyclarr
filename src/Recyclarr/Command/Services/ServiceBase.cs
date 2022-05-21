using System.Text;
using CliFx.Exceptions;
using Flurl.Http;
using TrashLib.Extensions;
using YamlDotNet.Core;

namespace Recyclarr.Command.Services;

/// <summary>
///     Mainly intended to handle common exception recovery logic between service command classes.
/// </summary>
public abstract class ServiceBase<T> where T : IServiceCommand
{
    public async Task Execute(T cmd)
    {
        try
        {
            await Process(cmd);
        }
        catch (YamlException e)
        {
            var message = e.InnerException is not null ? e.InnerException.Message : e.Message;
            var msg = new StringBuilder();
            msg.AppendLine($"Found Unrecognized YAML Property: {message}");
            msg.AppendLine("Please remove the property quoted in the above message from your YAML file");
            msg.AppendLine("Exiting due to invalid configuration");
            throw new CommandException(msg.ToString());
        }
        catch (FlurlHttpException e)
        {
            throw new CommandException(
                $"HTTP error while communicating with {cmd.Name}: {e.SanitizedExceptionMessage()}");
        }
        catch (Exception e) when (e is not CommandException)
        {
            throw new CommandException(e.ToString());
        }
    }

    protected abstract Task Process(T cmd);
}

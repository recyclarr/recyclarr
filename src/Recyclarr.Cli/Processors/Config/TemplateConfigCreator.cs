using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Cli.Processors.Config;

public class TemplateConfigCreator : IConfigCreator
{
    private readonly ILogger _log;
    private readonly IConfigTemplateGuideService _templates;

    private readonly IAppPaths _paths;
    // private readonly IConfigManipulator _configManipulator;
    // private readonly IAnsiConsole _console;

    public TemplateConfigCreator(
        ILogger log,
        IConfigTemplateGuideService templates,
        IAppPaths paths)
    {
        _log = log;
        _templates = templates;
        _paths = paths;
        // _configManipulator = configManipulator;
        // _console = console;
    }

    public bool CanHandle(ICreateConfigSettings settings)
    {
        return settings.Templates.Any();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public Task Create(ICreateConfigSettings settings)
    {
        _log.Debug("Creating config from templates: {Templates}", settings.Templates);

        var matchingTemplateData = _templates.LoadTemplateData()
            .IntersectBy(settings.Templates, path => path.Id, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => x.TemplateFile);

        foreach (var templateFile in matchingTemplateData)
        {
            var destinationFile = _paths.ConfigsDirectory.File(templateFile.Name);

            try
            {
                if (destinationFile.Exists && !settings.Force)
                {
                    throw new FileExistsException($"{destinationFile} already exists");
                }

                destinationFile.CreateParentDirectory();
                templateFile.CopyTo(destinationFile.FullName, true);

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (destinationFile.Exists)
                {
                    _log.Information("Replacing existing file: {Path}", destinationFile);
                }
                else
                {
                    _log.Information("Created configuration file: {Path}", destinationFile);
                }

                // -- See comment in ConfigManipulator.cs --
                // _configManipulator.LoadAndSave(templateFile, destinationFile, (instanceName, config) =>
                // {
                //     _console.MarkupLineInterpolated($"Enter configuration info for instance [green]{instanceName}[/]:");
                //     var baseUrl = _console.Prompt(new TextPrompt<string>("Base URL:"));
                //     var apiKey = _console.Prompt(new TextPrompt<string>("API Key:"));
                //     return config with
                //     {
                //         BaseUrl = baseUrl,
                //         ApiKey = apiKey
                //     };
                // });
            }
            catch (FileExistsException e)
            {
                _log.Error("Template configuration file could not be saved: {Reason}", e.AttemptedPath);
            }
            catch (FileLoadException)
            {
                // Do not log here since the origin of this exception is ConfigParser.Load(), which already has
                // sufficient logging.
            }
            catch (Exception e)
            {
                _log.Error(e, "Unable to save configuration template file");
            }
        }

        return Task.CompletedTask;
    }
}

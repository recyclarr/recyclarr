using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Guide;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Cli.Processors.Config;

public class TemplateConfigCreator : IConfigCreator
{
    private readonly ILogger _log;
    private readonly IConfigTemplateGuideService _templates;

    private readonly IAppPaths _paths;

    public TemplateConfigCreator(
        ILogger log,
        IConfigTemplateGuideService templates,
        IAppPaths paths)
    {
        _log = log;
        _templates = templates;
        _paths = paths;
    }

    public bool CanHandle(ICreateConfigSettings settings)
    {
        return settings.Templates.Any();
    }

    public void Create(ICreateConfigSettings settings)
    {
        _log.Debug("Creating config from templates: {Templates}", settings.Templates);

        var matchingTemplateData = _templates.GetTemplateData()
            .IntersectBy(settings.Templates, path => path.Id, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => x.TemplateFile);

        foreach (var templateFile in matchingTemplateData)
        {
            try
            {
                CopyTemplate(templateFile, settings);
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
                throw;
            }
        }
    }

    private void CopyTemplate(IFileInfo templateFile, ICreateConfigSettings settings)
    {
        var destinationFile = _paths.ConfigsDirectory.File(templateFile.Name);

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
    }
}

using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Processors.Config;

public class TemplateConfigCreator(
    ILogger log,
    IConfigTemplateGuideService templates,
    IAppPaths paths)
    : IConfigCreator
{
    public bool CanHandle(ICreateConfigSettings settings)
    {
        return settings.Templates.Count != 0;
    }

    public void Create(ICreateConfigSettings settings)
    {
        log.Debug("Creating config from templates: {Templates}", settings.Templates);

        var matchingTemplateData = templates.GetTemplateData()
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
                log.Error(e, "Template configuration file could not be saved");
            }
            catch (FileLoadException)
            {
                // Do not log here since the origin of this exception is ConfigParser.Load(), which already has
                // sufficient logging.
            }
            catch (Exception e)
            {
                log.Error(e, "Unable to save configuration template file");
                throw;
            }
        }
    }

    private void CopyTemplate(IFileInfo templateFile, ICreateConfigSettings settings)
    {
        var destinationFile = paths.ConfigsDirectory.File(templateFile.Name);

        if (destinationFile.Exists && !settings.Force)
        {
            throw new FileExistsException(destinationFile.FullName);
        }

        destinationFile.CreateParentDirectory();
        templateFile.CopyTo(destinationFile.FullName, true);

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (destinationFile.Exists)
        {
            log.Information("Replacing existing file: {Path}", destinationFile);
        }
        else
        {
            log.Information("Created configuration file: {Path}", destinationFile);
        }
    }
}

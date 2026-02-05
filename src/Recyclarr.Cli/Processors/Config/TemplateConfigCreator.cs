using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common.Extensions;
using Recyclarr.ConfigTemplates;
using Recyclarr.Platform;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Config;

internal class TemplateConfigCreator(
    ILogger log,
    IAnsiConsole console,
    ConfigTemplatesResourceQuery templates,
    IAppPaths paths
) : IConfigCreator
{
    public bool CanHandle(ICreateConfigSettings settings)
    {
        return settings.Templates.Count != 0;
    }

    public void Create(ICreateConfigSettings settings)
    {
        log.Debug("Creating config from templates: {Templates}", settings.Templates);

        var allTemplates = templates
            .Get(SupportedServices.Radarr)
            .Concat(templates.Get(SupportedServices.Sonarr));

        var matchingTemplateData = allTemplates
            .IntersectBy(
                settings.Templates,
                path => path.Id,
                StringComparer.CurrentCultureIgnoreCase
            )
            .Select(x => x.TemplateFile);

        foreach (var templateFile in matchingTemplateData)
        {
            try
            {
                CopyTemplate(templateFile, settings);
            }
            catch (FileLoadException)
            {
                // Do not log here since the origin of this exception is ConfigParser.Load(), which already has
                // sufficient logging.
            }
            catch (IOException e)
            {
                log.Error(e, "Unable to save configuration template file; skipping");
            }
        }
    }

    private void CopyTemplate(IFileInfo templateFile, ICreateConfigSettings settings)
    {
        var destinationFile = paths.YamlConfigDirectory.File(templateFile.Name);
        var alreadyExists = destinationFile.Exists;

        if (alreadyExists && !settings.Force)
        {
            throw new FileExistsException(destinationFile.FullName);
        }

        destinationFile.CreateParentDirectory();
        templateFile.CopyTo(destinationFile.FullName, true);

        if (alreadyExists)
        {
            log.Information("Replacing existing file: {Path}", destinationFile);
            console.MarkupLineInterpolated($"[yellow]Replaced:[/] {destinationFile.FullName}");
        }
        else
        {
            log.Information("Created configuration file: {Path}", destinationFile);
            console.MarkupLineInterpolated($"[green]Created:[/] {destinationFile.FullName}");
        }
    }
}

using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.ConfigTemplates;
using Recyclarr.Platform;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config;

internal class TemplateConfigCreator(
    ILogger log,
    ConfigTemplatesResourceQuery templates,
    IAppPaths paths
) : IConfigCreator
{
    public bool CanHandle(ICreateConfigSettings settings) => settings.Templates.Count != 0;

    public IReadOnlyList<CreatedConfigFile> Create(ICreateConfigSettings settings)
    {
        log.Debug("Creating config from templates: {Templates}", settings.Templates);

        var allTemplates = templates
            .Get(SupportedServices.Radarr)
            .Concat(templates.Get(SupportedServices.Sonarr));

        var matchingTemplateData = allTemplates.IntersectBy(
            settings.Templates,
            path => path.Id,
            StringComparer.CurrentCultureIgnoreCase
        );

        var results = new List<CreatedConfigFile>();

        foreach (var template in matchingTemplateData)
        {
            try
            {
                CopyTemplate(template, settings, results);
            }
            catch (FileLoadException)
            {
                // Do not log here since the origin of this exception is ConfigParser.Load(), which
                // already has sufficient logging.
            }
            catch (IOException e)
            {
                log.Error(e, "Unable to save configuration template file; skipping");
            }
        }

        return results;
    }

    private void CopyTemplate(
        ConfigTemplateResource template,
        ICreateConfigSettings settings,
        List<CreatedConfigFile> results
    )
    {
        var destinationFile = paths.YamlConfigDirectory.File($"{template.Id}.yml");
        var alreadyExists = destinationFile.Exists;

        if (alreadyExists && !settings.Force)
        {
            throw new FileExistsException(destinationFile.FullName);
        }

        destinationFile.CreateParentDirectory();
        template.TemplateFile.CopyTo(destinationFile.FullName, true);

        if (alreadyExists)
        {
            log.Information("Replacing existing file: {Path}", destinationFile);
        }
        else
        {
            log.Information("Created configuration file: {Path}", destinationFile);
        }
        results.Add(new CreatedConfigFile(destinationFile.FullName, alreadyExists));
    }
}

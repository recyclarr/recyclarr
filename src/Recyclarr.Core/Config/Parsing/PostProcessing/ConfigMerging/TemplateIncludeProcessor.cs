using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.ConfigTemplates;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class TemplateIncludeProcessor(ConfigIncludesResourceQuery includes) : IIncludeProcessor
{
    public IFileInfo GetPathToConfig(IYamlInclude includeDirective, SupportedServices serviceType)
    {
        var include = (TemplateYamlInclude)includeDirective;

        if (include.Template is null)
        {
            throw new YamlIncludeException("`template` property is required.");
        }

        IEnumerable<ConfigIncludeResource> includesForService = serviceType switch
        {
            SupportedServices.Radarr => includes.GetRadarr(),
            SupportedServices.Sonarr => includes.GetSonarr(),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType)),
        };

        var includePath = includesForService.LastOrDefault(x =>
            x.Id.EqualsIgnoreCase(include.Template)
        );

        if (includePath is null)
        {
            throw new YamlIncludeException(
                $"For service type '{serviceType}', unable to find config include with name '{include.Template}'"
            );
        }

        return includePath.TemplateFile;
    }
}

using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing.PostProcessing.ConfigMerging;

public class TemplateIncludeProcessor : IIncludeProcessor
{
    private readonly IConfigTemplateGuideService _templates;

    public TemplateIncludeProcessor(IConfigTemplateGuideService templates)
    {
        _templates = templates;
    }

    public bool CanProcess(IYamlInclude includeDirective)
    {
        return includeDirective is TemplateYamlInclude;
    }

    public IFileInfo GetPathToConfig(IYamlInclude includeDirective, SupportedServices serviceType)
    {
        var include = (TemplateYamlInclude) includeDirective;

        if (include.Template is null)
        {
            throw new YamlIncludeException("`template` property is required.");
        }

        var includePath = _templates.GetIncludeData()
            .Where(x => x.Service == serviceType)
            .FirstOrDefault(x => x.Id.EqualsIgnoreCase(include.Template));

        if (includePath is null)
        {
            throw new YamlIncludeException(
                $"For service type '{serviceType}', unable to find config template with name '{include.Template}'");
        }

        return includePath.TemplateFile;
    }
}

using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.ConfigTemplates;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class TemplateIncludeProcessor(IConfigIncludesResourceQuery includes) : IIncludeProcessor
{
    public IFileInfo GetPathToConfig(IYamlInclude includeDirective, SupportedServices serviceType)
    {
        var include = (TemplateYamlInclude)includeDirective;

        if (include.Template is null)
        {
            throw new YamlIncludeException("`template` property is required.");
        }

        var includePath = includes
            .GetIncludes()
            .Where(x => x.Service == serviceType)
            .FirstOrDefault(x => x.Id.EqualsIgnoreCase(include.Template));

        if (includePath is null)
        {
            throw new YamlIncludeException(
                $"For service type '{serviceType}', unable to find config include with name '{include.Template}'"
            );
        }

        return includePath.TemplateFile;
    }
}

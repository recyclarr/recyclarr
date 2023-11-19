using JetBrains.Annotations;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.Config.Parsing;

[UsedImplicitly]
public class ConfigValidationExecutor(ILogger log, IRuntimeValidationService validationService)
{
    public bool Validate(object config, params string[] ruleSets)
    {
        var result = validationService.Validate(config, ruleSets);
        if (result.IsValid)
        {
            return true;
        }

        var numErrors = result.Errors.LogValidationErrors(log, "Config Validation");
        if (numErrors == 0)
        {
            return true;
        }

        log.Error("Config validation failed with {Count} errors", numErrors);
        return false;
    }
}

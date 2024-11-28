using Recyclarr.Common.FluentValidation;

namespace Recyclarr.Config.Parsing;

[UsedImplicitly]
public class ConfigValidationExecutor(
    ValidationLogger validationLogger,
    IRuntimeValidationService validationService
)
{
    public bool Validate(object config, params string[] ruleSets)
    {
        var result = validationService.Validate(config, ruleSets);
        if (result.IsValid)
        {
            return true;
        }

        return !validationLogger.LogValidationErrors(result.Errors, "Config Validation");
    }
}

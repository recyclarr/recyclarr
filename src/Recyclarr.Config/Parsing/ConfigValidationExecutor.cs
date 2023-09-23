using JetBrains.Annotations;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.Config.Parsing;

[UsedImplicitly]
public class ConfigValidationExecutor
{
    private readonly ILogger _log;
    private readonly IRuntimeValidationService _validationService;

    public ConfigValidationExecutor(ILogger log, IRuntimeValidationService validationService)
    {
        _log = log;
        _validationService = validationService;
    }

    public bool Validate(object config, params string[] ruleSets)
    {
        var result = _validationService.Validate(config, ruleSets);
        if (result.IsValid)
        {
            return true;
        }

        var numErrors = result.Errors.LogValidationErrors(_log, "Config Validation");
        if (numErrors == 0)
        {
            return true;
        }

        _log.Error("Config validation failed with {Count} errors", numErrors);
        return false;
    }
}

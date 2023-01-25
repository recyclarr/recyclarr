using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigValidationExecutor
{
    private readonly ILogger _log;
    private readonly IValidator<ServiceConfiguration> _validator;

    public ConfigValidationExecutor(
        ILogger log,
        IValidator<ServiceConfiguration> validator)
    {
        _log = log;
        _validator = validator;
    }

    public bool Validate(ServiceConfiguration config)
    {
        var result = _validator.Validate(config);
        if (result is not {IsValid: false})
        {
            return true;
        }

        var printableName = config.InstanceName ?? FlurlLogging.SanitizeUrl(config.BaseUrl);
        _log.Error("Validation failed for instance config {Instance} at line {Line} with {Count} errors",
            printableName, config.LineNumber, result.Errors.Count);

        foreach (var error in result.Errors)
        {
            _log.Error("Validation error: {Msg}", error.ErrorMessage);
        }

        return false;
    }
}

using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.Common.FluentValidation;
using Serilog.Events;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigValidationExecutor
{
    private readonly ILogger _log;
    private readonly RuntimeValidationService _validationService;

    public ConfigValidationExecutor(ILogger log, RuntimeValidationService validationService)
    {
        _log = log;
        _validationService = validationService;
    }

    public bool Validate(object config)
    {
        var result = _validationService.Validate(config);
        if (result.IsValid)
        {
            return true;
        }

        var anyErrorsDetected = false;

        foreach (var error in result.Errors)
        {
            var level = error.Severity switch
            {
                Severity.Error => LogEventLevel.Error,
                Severity.Warning => LogEventLevel.Warning,
                Severity.Info => LogEventLevel.Information,
                _ => LogEventLevel.Debug
            };

            anyErrorsDetected |= level == LogEventLevel.Error;
            _log.Write(level, "Config Validation: {Msg}", error.ErrorMessage);
        }

        if (anyErrorsDetected)
        {
            _log.Error("Config validation failed with {Count} errors", result.Errors.Count);
        }

        return !anyErrorsDetected;
    }
}

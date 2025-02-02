using FluentValidation.Results;

namespace Recyclarr.Config.ExceptionTypes;

public record ConfigValidationErrorInfo(
    string InstanceName,
    IReadOnlyCollection<ValidationFailure> Failures
);

public class ConfigValidationException(
    IReadOnlyCollection<ConfigValidationErrorInfo> invalidConfigs
) : InvalidConfigurationException
{
    public IReadOnlyCollection<ConfigValidationErrorInfo> InvalidConfigs => invalidConfigs;
}

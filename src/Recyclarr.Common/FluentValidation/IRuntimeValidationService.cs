using FluentValidation.Results;

namespace Recyclarr.Common.FluentValidation;

public interface IRuntimeValidationService
{
    ValidationResult Validate(object instance);
}

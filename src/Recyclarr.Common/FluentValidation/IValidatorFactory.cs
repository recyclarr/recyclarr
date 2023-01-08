using FluentValidation;

namespace Recyclarr.Common.FluentValidation;

public interface IValidatorFactory
{
    IValidator GetValidator(Type typeToValidate);
}

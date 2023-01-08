using Autofac;
using FluentValidation;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Common.FluentValidation;

public class ValidatorFactory : IValidatorFactory
{
    private readonly ILifetimeScope _scope;

    public ValidatorFactory(ILifetimeScope scope)
    {
        _scope = scope;
    }

    public IValidator GetValidator(Type typeToValidate)
    {
        return (IValidator) _scope.ResolveGeneric(typeof(IValidator<>), typeToValidate);
    }
}

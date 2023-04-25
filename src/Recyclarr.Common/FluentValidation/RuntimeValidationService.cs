using FluentValidation;
using FluentValidation.Results;

namespace Recyclarr.Common.FluentValidation;

public class RuntimeValidationService : IRuntimeValidationService
{
    private readonly Dictionary<Type, IValidator> _validators;

    private static Type? GetValidatorInterface(Type type)
    {
        return type.GetInterfaces()
            .FirstOrDefault(i
                => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));
    }

    public RuntimeValidationService(IEnumerable<IValidator> validators)
    {
        _validators = validators
            .Select(x => (x, GetValidatorInterface(x.GetType())))
            .Where(x => x.Item2 is not null)
            .ToDictionary(x => x.Item2!.GetGenericArguments()[0], x => x.Item1);
    }

    public ValidationResult Validate(object instance)
    {
        if (!_validators.TryGetValue(instance.GetType(), out var validator))
        {
            throw new ValidationException($"No validator is available for type: {instance.GetType().FullName}");
        }

        return validator.Validate(new ValidationContext<object>(instance));
    }
}

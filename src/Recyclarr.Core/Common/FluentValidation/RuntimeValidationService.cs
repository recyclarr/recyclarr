using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;

namespace Recyclarr.Common.FluentValidation;

public class RuntimeValidationService(IEnumerable<IValidator> validators)
    : IRuntimeValidationService
{
    private readonly Dictionary<Type, IValidator> _validators = validators
        .Select(x => (x, GetValidatorInterface(x.GetType())))
        .Where(x => x.Item2 is not null)
        .ToDictionary(x => x.Item2!.GetGenericArguments()[0], x => x.Item1);

    private static Type? GetValidatorInterface(Type type)
    {
        return Array.Find(
            type.GetInterfaces(),
            i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)
        );
    }

    public ValidationResult Validate(object instance, params string[] additionalRuleSets)
    {
        if (!_validators.TryGetValue(instance.GetType(), out var validator))
        {
            throw new ValidationException(
                $"No validator is available for type: {instance.GetType().FullName}"
            );
        }

        var validatorSelector = new RulesetValidatorSelector([
            RulesetValidatorSelector.DefaultRuleSetName,
            .. additionalRuleSets,
        ]);

        return validator.Validate(
            new ValidationContext<object>(instance, new PropertyChain(), validatorSelector)
        );
    }
}

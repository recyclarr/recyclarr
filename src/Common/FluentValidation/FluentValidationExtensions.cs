using FluentValidation;
using FluentValidation.Results;

namespace Common.FluentValidation;

public static class FluentValidationExtensions
{
    // From: https://github.com/FluentValidation/FluentValidation/issues/1648
    public static IRuleBuilderOptions<T, TProperty?> SetNonNullableValidator<T, TProperty>(
        this IRuleBuilder<T, TProperty?> ruleBuilder, IValidator<TProperty> validator, params string[] ruleSets)
    {
        var adapter = new NullableChildValidatorAdaptor<T, TProperty>(validator, validator.GetType())
        {
            RuleSets = ruleSets
        };

        return ruleBuilder.SetAsyncValidator(adapter);
    }

    public static IEnumerable<TSource> IsValid<TSource, TValidator>(
        this IEnumerable<TSource> source, TValidator validator,
        Action<List<ValidationFailure>, TSource>? handleInvalid = null)
        where TValidator : IValidator<TSource>, new()
    {
        foreach (var s in source)
        {
            var result = validator.Validate(s);
            if (result.IsValid)
            {
                yield return s;
            }
            else
            {
                handleInvalid?.Invoke(result.Errors, s);
            }
        }
    }
}

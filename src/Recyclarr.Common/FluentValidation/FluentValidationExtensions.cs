using FluentValidation;
using FluentValidation.Results;

namespace Recyclarr.Common.FluentValidation;

public static class FluentValidationExtensions
{
    // From: https://github.com/FluentValidation/FluentValidation/issues/1648
    // ReSharper disable once UnusedMethodReturnValue.Global
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
        where TValidator : IValidator<TSource>
    {
        foreach (var s in source)
        {
            var result = validator.Validate(s);
            if (result is {IsValid: true})
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

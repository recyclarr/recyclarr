using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Validators;

namespace Common.Extensions;

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

    private sealed class NullableChildValidatorAdaptor<T, TProperty> : ChildValidatorAdaptor<T, TProperty>,
        IPropertyValidator<T, TProperty?>, IAsyncPropertyValidator<T, TProperty?>
    {
        public NullableChildValidatorAdaptor(IValidator<TProperty> validator, Type validatorType)
            : base(validator, validatorType)
        {
        }

        public override Task<bool> IsValidAsync(ValidationContext<T> context, TProperty? value,
            CancellationToken cancellation)
        {
            return base.IsValidAsync(context, value!, cancellation);
        }

        public override bool IsValid(ValidationContext<T> context, TProperty? value)
        {
            return base.IsValid(context, value!);
        }
    }
}

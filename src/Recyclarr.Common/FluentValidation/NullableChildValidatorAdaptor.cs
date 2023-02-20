using FluentValidation;
using FluentValidation.Validators;

namespace Recyclarr.Common.FluentValidation;

internal sealed class NullableChildValidatorAdaptor<T, TProperty> : ChildValidatorAdaptor<T, TProperty>,
    IPropertyValidator<T, TProperty?>, IAsyncPropertyValidator<T, TProperty?>
{
    public NullableChildValidatorAdaptor(IValidator<TProperty> validator, Type validatorType)
        : base(validator, validatorType)
    {
    }

    public override Task<bool> IsValidAsync(
        ValidationContext<T> context,
        TProperty? value,
        CancellationToken cancellation)
    {
        return base.IsValidAsync(context, value!, cancellation);
    }

    public override bool IsValid(ValidationContext<T> context, TProperty? value)
    {
        return base.IsValid(context, value!);
    }
}

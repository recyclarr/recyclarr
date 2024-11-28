using FluentValidation;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Common.FluentValidation;

public abstract class CustomValidator<T> : AbstractValidator<T>
{
    public bool OnlyOneHasElements<TC1, TC2>(IEnumerable<TC1>? c1, IEnumerable<TC2>? c2)
    {
        var notEmpty = new[] { c1.IsNotEmpty(), c2.IsNotEmpty() };

        return notEmpty.Count(x => x) <= 1;
    }
}

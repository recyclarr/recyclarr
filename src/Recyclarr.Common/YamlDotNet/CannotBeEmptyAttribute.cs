using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Recyclarr.Common.YamlDotNet;

[AttributeUsage(AttributeTargets.Property)]
// ReSharper disable once UnusedType.Global
public sealed class CannotBeEmptyAttribute : RequiredAttribute
{
    public override bool IsValid(object? value)
    {
        return base.IsValid(value) &&
               value is IEnumerable list &&
               list.GetEnumerator().MoveNext();
    }
}

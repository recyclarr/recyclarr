using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Yaml.YamlDotNet;

[AttributeUsage(AttributeTargets.Property)]
// ReSharper disable once UnusedType.Global
public sealed class CannotBeEmptyAttribute : RequiredAttribute
{
    [SuppressMessage("ReSharper", "NotDisposedResource")]
    public override bool IsValid(object? value)
    {
        return base.IsValid(value) && value is IEnumerable list && list.GetEnumerator().MoveNext();
    }
}

using System.Diagnostics.CodeAnalysis;
using Autofac.Features.Indexed;

namespace Recyclarr.TestLibrary.Autofac;

public class StubAutofacIndex<TKey, TValue> : IIndex<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _values = new();

    public void Add(TKey key, TValue value)
    {
        _values.Add(key, value);
    }

    public bool TryGetValue(TKey key, [UnscopedRef] out TValue value)
    {
        return _values.TryGetValue(key, out value!);
    }

    public TValue this[TKey key] => _values[key];
}

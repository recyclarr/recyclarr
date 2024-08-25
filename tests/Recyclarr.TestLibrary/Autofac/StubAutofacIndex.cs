using System.Diagnostics.CodeAnalysis;
using Autofac.Features.Indexed;

namespace Recyclarr.TestLibrary.Autofac;

[UsedImplicitly]
public class StubAutofacIndex<TKey, TValue> : IIndex<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _values = new();

    public StubAutofacIndex()
    {
    }

    public StubAutofacIndex(Dictionary<TKey, TValue> values)
    {
        _values = values;
    }

    public void Add(TKey key, TValue value)
    {
        _values.Add(key, value);
    }

    public void AddRange(IEnumerable<(TKey, TValue)> pairs)
    {
        foreach (var (key, value) in pairs)
        {
            _values[key] = value;
        }
    }

    public bool TryGetValue(TKey key, [UnscopedRef] out TValue value)
    {
        return _values.TryGetValue(key, out value!);
    }

    public TValue this[TKey key] => _values[key];
    public IReadOnlyCollection<TKey> Keys => _values.Keys;
    public IReadOnlyCollection<TValue> Values => _values.Values;
}

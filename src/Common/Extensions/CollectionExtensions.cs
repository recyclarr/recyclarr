using System.Collections;

namespace Common.Extensions;

public static class CollectionExtensions
{
    // From: https://stackoverflow.com/a/34362585/157971
    public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source as IReadOnlyCollection<T> ?? new ReadOnlyCollectionAdapter<T>(source);
    }

    // From: https://stackoverflow.com/a/34362585/157971
    private sealed class ReadOnlyCollectionAdapter<T> : IReadOnlyCollection<T>
    {
        private readonly ICollection<T> _source;
        public ReadOnlyCollectionAdapter(ICollection<T> source) => _source = source;
        public int Count => _source.Count;
        public IEnumerator<T> GetEnumerator() => _source.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            destination.Add(item);
        }
    }
}

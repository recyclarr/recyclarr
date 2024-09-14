namespace Recyclarr.Common.Extensions;

#pragma warning disable CS8851
public static class HashCodeExtensions
{
    public static int CalcHashCode<T>(this IEnumerable<T> source)
    {
        return source.Aggregate(new HashCode(), (hash, item) =>
        {
            hash.Add(item);
            return hash;
        }).ToHashCode();
    }
}

using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Common;

[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
public sealed class GenericEqualityComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T, T, bool> _equalsPredicate;
    private readonly Func<T, int> _hashPredicate;

    public GenericEqualityComparer(Func<T, T, bool> equalsPredicate, Func<T, int> hashPredicate)
    {
        _equalsPredicate = equalsPredicate;
        _hashPredicate = hashPredicate;
    }

    public bool Equals(T? x, T? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null))
        {
            return false;
        }

        if (ReferenceEquals(y, null))
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return _equalsPredicate(x, y);
    }

    public int GetHashCode(T obj)
    {
        return _hashPredicate(obj);
    }
}

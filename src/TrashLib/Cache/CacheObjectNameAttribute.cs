namespace TrashLib.Cache;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class CacheObjectNameAttribute : Attribute
{
    public CacheObjectNameAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

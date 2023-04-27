namespace Recyclarr.TrashLib.Cache;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheObjectNameAttribute : Attribute
{
    public CacheObjectNameAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

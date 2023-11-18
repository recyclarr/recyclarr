namespace Recyclarr.Cli.Cache;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheObjectNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

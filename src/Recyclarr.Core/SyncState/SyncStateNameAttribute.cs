namespace Recyclarr.SyncState;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SyncStateNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

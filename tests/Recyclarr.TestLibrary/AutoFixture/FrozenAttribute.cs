namespace Recyclarr.TestLibrary.AutoFixture;

/// <summary>
/// Marks a test method parameter to be frozen, ensuring the same instance is reused
/// for all requests matching the specified criteria within the test.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FrozenAttribute(Matching by = Matching.ExactType) : Attribute
{
    public Matching By { get; } = by;
}

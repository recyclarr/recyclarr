namespace Recyclarr.TestLibrary.AutoFixture;

/// <summary>
/// Specifies how a frozen specimen should be matched when injected into other specimens.
/// </summary>
[Flags]
public enum Matching
{
    /// <summary>
    /// Match requests for the exact same type.
    /// </summary>
    ExactType = 1,

    /// <summary>
    /// Match requests for any interface implemented by the frozen type.
    /// </summary>
    ImplementedInterfaces = 2,
}

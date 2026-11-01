namespace Recyclarr.Platform;

public static class RecyclarrVersion
{
    /// <summary>The major version of this build; 0 when the assembly carries no version.</summary>
    public static int Major { get; } =
        typeof(RecyclarrVersion).Assembly.GetName().Version?.Major ?? 0;
}

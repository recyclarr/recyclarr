using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;

namespace Recyclarr.ResourceProviders.Storage;

/// <summary>
/// What Recyclarr last did to a remote source's cache. It lives outside the provider's directory so
/// resource loading never sees it and a cache rebuild (which deletes that directory) keeps it.
/// </summary>
public sealed record ProviderRefreshState
{
    public required DateTimeOffset LastAttemptAt { get; init; }
    public required bool LastAttemptSucceeded { get; init; }
    public DateTimeOffset? LastSuccessAt { get; init; }

    /// <summary>The <see cref="SourceTracking.Identity"/> these times apply to.</summary>
    public required string SourceIdentity { get; init; }

    /// <summary>
    /// The stored state, or null when the file is missing or unreadable. A null state means the
    /// cache is stale.
    /// </summary>
    public static ProviderRefreshState? Read(IFileInfo file)
    {
        file.Refresh();
        if (!file.Exists)
        {
            return null;
        }

        try
        {
            using var stream = file.OpenRead();
            return JsonSerializer.Deserialize<ProviderRefreshState>(
                stream,
                GlobalJsonSerializerSettings.Recyclarr
            );
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            return null;
        }
    }

    // Written to a temporary file and moved into place so a crash never leaves a partial file.
    public void Write(IFileInfo file)
    {
        file.Directory?.Create();
        var temp = file.FileSystem.FileInfo.New(file.FullName + ".tmp");
        using (var stream = temp.Create())
        {
            JsonSerializer.Serialize(stream, this, GlobalJsonSerializerSettings.Recyclarr);
        }

        temp.MoveTo(file.FullName, overwrite: true);
    }
}

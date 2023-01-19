using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Services.ReleaseProfile.Guide;

public record ReleaseProfilePaths(
    IReadOnlyList<IDirectoryInfo> ReleaseProfileDirectories
);

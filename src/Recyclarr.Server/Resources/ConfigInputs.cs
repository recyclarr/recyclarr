using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Security.Cryptography;
using Recyclarr.Platform;

namespace Recyclarr.Server.Resources;

/// <summary>
/// Finds and fingerprints the configuration inputs (ADR-019). Discovery covers the files a load
/// starts from; paths a load reads beyond those come from <c>ConfigInputRecorder</c>.
/// </summary>
internal sealed class ConfigInputs(IFileSystem fs, IAppPaths paths)
{
    // An unreadable file (for example, mid-write) never equals a readable one, so the next check
    // sees a change once it can be read.
    private const string Unreadable = "unreadable";

    private static readonly string[] RootFileNames =
    [
        "recyclarr.yml",
        "recyclarr.yaml",
        "secrets.yml",
        "secrets.yaml",
    ];

    private static readonly string[] YamlPatterns = ["*.yml", "*.yaml"];

    /// <summary>
    /// Every path a configuration load starts from, whether or not the file exists: the files in
    /// the config subdirectory, plus the root config and secrets file names.
    /// </summary>
    public IReadOnlyCollection<string> Discover()
    {
        var candidates = RootFileNames
            .Select(name => paths.ConfigDirectory.File(name).FullName)
            .ToList();

        var configs = paths.YamlConfigDirectory;
        if (configs.Exists)
        {
            candidates.AddRange(
                YamlPatterns.SelectMany(configs.EnumerateFiles).Select(f => f.FullName)
            );
        }

        return candidates;
    }

    /// <summary>
    /// Hashes each path now, except paths already hashed in <paramref name="reuse"/>, which keep
    /// that hash.
    /// </summary>
    public ConfigInputFingerprint Capture(
        IEnumerable<string> inputPaths,
        ConfigInputFingerprint? reuse = null
    )
    {
        var hashes = ImmutableSortedDictionary.CreateBuilder<string, string?>(
            StringComparer.Ordinal
        );
        foreach (var path in inputPaths)
        {
            if (hashes.ContainsKey(path))
            {
                continue;
            }

            hashes[path] =
                reuse?.Hashes.TryGetValue(path, out var known) == true ? known : Hash(path);
        }

        return new ConfigInputFingerprint(hashes.ToImmutable());
    }

    private string? Hash(string path)
    {
        var file = fs.FileInfo.New(path);
        if (!file.Exists)
        {
            return null;
        }

        try
        {
            using var stream = file.OpenRead();
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Unreadable;
        }
    }
}

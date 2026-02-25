using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Migration.Steps;

/// <summary>
/// Renames the top-level JSON property in state mapping files from the old per-type names
/// ("trash_id_mappings", "TrashIdMappings") to the canonical name ("mappings").
/// </summary>
[UsedImplicitly]
[MigrationOrder(3)]
internal class NormalizeStateMappingsPropertyStep(IAppPaths paths) : IMigrationStep
{
    private static readonly string[] LegacyPropertyNames = ["trash_id_mappings", "TrashIdMappings"];

    public string Description => "Normalize state mapping file property names";

    public IReadOnlyCollection<string> Remediation =>
        [
            "Run 'recyclarr state repair' to rebuild state files from scratch",
            $"Manually edit JSON files under {paths.StateDirectory} and rename the top-level property to \"mappings\"",
        ];

    public bool CheckIfNeeded()
    {
        return FindFilesNeedingMigration().Any();
    }

    public void Execute(ILogger log)
    {
        foreach (var file in FindFilesNeedingMigration())
        {
            var json = file.FileSystem.File.ReadAllText(file.FullName);
            var node = JsonNode.Parse(json);
            if (node is not JsonObject obj)
            {
                continue;
            }

            foreach (var legacyName in LegacyPropertyNames)
            {
                if (!obj.TryGetPropertyValue(legacyName, out var value))
                {
                    continue;
                }

                // Move the array to the canonical name and remove the old property
                obj.Remove(legacyName);
                obj["mappings"] = value?.DeepClone();
                break;
            }

            // Remove the "version" property if present (legacy artifact)
            obj.Remove("version");

            var options = new JsonSerializerOptions { WriteIndented = true };
            file.FileSystem.File.WriteAllText(file.FullName, obj.ToJsonString(options));

            log.Information("Normalized property name in {File}", file.FullName);
        }
    }

    private IEnumerable<IFileInfo> FindFilesNeedingMigration()
    {
        var stateDir = paths.StateDirectory;
        if (!stateDir.Exists)
        {
            yield break;
        }

        // State files live at: state/{sonarr|radarr}/{hash}/*-mappings.json
        foreach (var serviceDir in stateDir.EnumerateDirectories())
        {
            foreach (var hashDir in serviceDir.EnumerateDirectories())
            {
                foreach (var file in hashDir.EnumerateFiles("*-mappings.json"))
                {
                    if (FileNeedsMigration(file))
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    private static bool FileNeedsMigration(IFileInfo file)
    {
        try
        {
            using var stream = file.OpenRead();
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            // Needs migration if it has a legacy property but not the canonical one
            var hasCanonical = root.TryGetProperty("mappings", out _);
            if (hasCanonical)
            {
                return false;
            }

            return LegacyPropertyNames.Any(name => root.TryGetProperty(name, out _));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

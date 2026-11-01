using Recyclarr.Platform;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.ConfigTemplates;

/// <summary>
/// Writes the YAML starter configuration for a <see cref="ConfigTemplateEntry"/>.
/// </summary>
/// <remarks>
/// The output reproduces the layout of the retired config-templates repository, which was reviewed
/// for readability: most of the file is commented-out YAML that documents every choice the profile
/// offers, so users enable options by uncommenting lines. No YAML library can emit commented-out
/// nodes, so the text is written line by line.
/// </remarks>
public class ConfigTemplateRenderer(CfGroupResourceQuery cfGroups, ConflictsResourceQuery conflicts)
{
    private const string Border =
        "################################################################################";

    private const string AddBanner = """
              ################################################################################
              ## Uncommented groups are synced by default. They are listed here so you can
              ## adjust which CFs they include. Commented groups are NOT synced; uncomment one
              ## to enable it.
              ##
              ## Within a group, uncomment a line under `select:` to turn an optional CF on,
              ## or under `exclude:` to turn a default CF off. Required CFs are always
              ## included and are not listed.
              ##
              ## To uncomment, remove `# ` (hash + space) so that indentation stays aligned.
              ## Most editors do this automatically with toggle-comment (Ctrl+/).
              ##
              ## https://recyclarr.dev/guide/cf-groups/
              ################################################################################
              add:
        """;

    private const string SkipBanner = """
              ################################################################################
              ## These groups ARE synced by default. Uncomment to disable.
              ##
              ## https://recyclarr.dev/guide/cf-groups/
              ################################################################################
              skip:
        """;

    public string Render(ConfigTemplateEntry template)
    {
        var profile = template.Profile;
        var profileGroups = cfGroups
            .Get(template.Service)
            .Where(g => g.IncludedProfileTrashIds.Contains(profile.TrashId))
            .OrderBy(g => g.Name, StringComparer.Ordinal)
            .ToList();
        var conflictSets = conflicts.Get(template.Service);

        var optionalGroups = profileGroups.Where(g => !g.IsDefault).ToList();
        var choiceGroups = profileGroups
            .Where(g => g.IsDefault && g.CustomFormats.Any(cf => cf.Default))
            .ToList();
        var defaultGroups = profileGroups.Except(optionalGroups).Except(choiceGroups).ToList();

        List<string> lines =
        [
            "# yaml-language-server: "
                + $"$schema=https://schemas.recyclarr.dev/v{RecyclarrVersion.Major}/config-schema.json",
            Border,
            $"## TRaSH Guides: {profile.Name}",
        ];

        // Links stay on one line so they remain clickable.
        if (profile.TrashUrl.Length > 0)
        {
            lines.Add("##");
            lines.Add($"## {profile.TrashUrl}");
        }

        lines.Add(Border);
        lines.Add("");

        var instance = $"""
            {ConfigTemplateCatalog.ServiceKey(template.Service)}:
              {template.Id}:
                base_url: Put your {template.Service} URL here
                api_key: Put your API key here

                quality_definition:
                  type: {template.QualityDefinitionType}

                quality_profiles:
                  - trash_id: {profile.TrashId}  # {profile.Name}
                    reset_unmatched_scores:
                      enabled: true
            """;
        lines.AddRange(SplitLines(instance));

        if (profileGroups.Count > 0)
        {
            lines.Add("");
            lines.Add("    custom_format_groups:");
        }

        if (optionalGroups.Count > 0 || choiceGroups.Count > 0)
        {
            lines.AddRange(SplitLines(AddBanner));
            lines.AddRange(
                choiceGroups.SelectMany(g => RenderGroupEntry(g, conflictSets, commented: false))
            );

            // Groups with no CFs to toggle come first so the longer entries sit together below.
            lines.AddRange(
                optionalGroups
                    .OrderBy(HasToggleableCfs)
                    .SelectMany(g => RenderGroupEntry(g, conflictSets, commented: true))
            );
        }

        if (defaultGroups.Count > 0)
        {
            lines.Add("");
            lines.AddRange(SplitLines(SkipBanner));
            lines.AddRange(defaultGroups.Select(g => $"        # - {g.TrashId}  # {g.Name}"));
        }

        lines.Add("");
        return string.Join('\n', lines);
    }

    // Source line endings vary by checkout; the output always uses LF.
    private static string[] SplitLines(string text) => text.ReplaceLineEndings("\n").Split('\n');

    private static bool HasToggleableCfs(CfGroupResource group) =>
        group.CustomFormats.Any(cf => !cf.Required);

    // Renders one entry under `add:`. An uncommented entry is a group synced by default; its
    // default CFs are listed commented under `exclude:` (uncomment to turn off) and its optional
    // CFs commented under `select:` (uncomment to turn on). A commented entry is an opt-in group.
    // Inside it, `exclude:` members carry an extra `# ` so enabling the group keeps its defaults,
    // while `select:` members sit at the block's own level so enabling the group includes them.
    // Required CFs are never listed because they cannot be excluded.
    private static IEnumerable<string> RenderGroupEntry(
        CfGroupResource group,
        IReadOnlyList<IReadOnlySet<string>> conflictSets,
        bool commented
    )
    {
        var prefix = commented ? "        # " : "        ";
        yield return $"{prefix}- trash_id: {group.TrashId}  # {group.Name}";

        var conflicting = conflictSets
            .Select(set => group.CustomFormats.Where(cf => set.Contains(cf.TrashId)).ToList())
            .Where(members => members.Count >= 2)
            .ToList();
        if (conflicting.Count > 0)
        {
            yield return $"{prefix}  # Mutually exclusive: enable only one of the following.";
        }

        // In an uncommented group every listed CF is a commented-out choice. In a commented group,
        // lines are active as the group would sync once uncommented: defaults stay included
        // unless they lose a conflict, and optionals are selected unless they lose one.
        var losers = commented ? ConflictLosers(conflicting) : [];
        var defaults = group.CustomFormats.Where(cf => cf.Default);
        var optionals = group.CustomFormats.Where(cf => cf.IsOptional);

        foreach (var line in CfList("exclude", defaults, cf => losers.Contains(cf.TrashId)))
        {
            yield return prefix + line;
        }

        foreach (var line in CfList("select", optionals, cf => !losers.Contains(cf.TrashId)))
        {
            yield return prefix + line;
        }

        IEnumerable<string> CfList(
            string node,
            IEnumerable<CfGroupCustomFormat> cfs,
            Func<CfGroupCustomFormat, bool> activeWhenCommented
        )
        {
            var entries = cfs.Select(cf =>
                    commented && activeWhenCommented(cf)
                        ? $"    - {cf.TrashId}  # {cf.Name}"
                        : $"    # - {cf.TrashId}  # {cf.Name}"
                )
                .ToList();

            return entries.Count == 0 ? [] : [$"  {node}:", .. entries];
        }
    }

    // Each conflict keeps one member enabled: a required CF, else the first default CF, else the
    // first member in guide order. Returns the trash_ids of the members to disable.
    private static HashSet<string> ConflictLosers(List<List<CfGroupCustomFormat>> conflicts)
    {
        return conflicts
            .SelectMany(members =>
            {
                var winner =
                    members.FirstOrDefault(cf => cf.Required)
                    ?? members.FirstOrDefault(cf => cf.Default)
                    ?? members[0];
                return members.Where(cf => cf != winner && !cf.Required);
            })
            .Select(cf => cf.TrashId)
            .ToHashSet();
    }
}

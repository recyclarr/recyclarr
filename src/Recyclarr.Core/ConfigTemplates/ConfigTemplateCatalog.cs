using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;

namespace Recyclarr.ConfigTemplates;

/// <summary>
/// A starter configuration that can be generated for one guide quality profile.
/// </summary>
/// <remarks>
/// <c>Id</c> is the profile's guide file name without extension. When both services have a profile
/// file with the same name, each id gets a <c>radarr-</c> or <c>sonarr-</c> prefix.
/// </remarks>
public record ConfigTemplateEntry(
    string Id,
    SupportedServices Service,
    QualityProfileResource Profile,
    string QualityDefinitionType
);

/// <summary>
/// Derives the available config templates from guide quality profiles. Template ids and the
/// excluded profile groups match those of the retired config-templates repository, so v8 template
/// ids remain valid.
/// </summary>
public class ConfigTemplateCatalog(
    QualityProfileResourceQuery profiles,
    QualityProfileGroupResourceQuery profileGroups,
    QualitySizeResourceQuery qualitySizes
)
{
    // The guide's TEST group holds a testing profile, not one meant for users (PDR-007).
    private const string ExcludedProfileGroup = "TEST";

    public IReadOnlyList<ConfigTemplateEntry> GetAll()
    {
        return Enum.GetValues<SupportedServices>()
            .SelectMany(GetServiceTemplates)
            .GroupBy(t => t.Id)
            .SelectMany(sameId =>
                sameId.Count() == 1
                    ? sameId
                    : sameId.Select(t => t with { Id = $"{ServiceKey(t.Service)}-{t.Id}" })
            )
            .ToList();
    }

    public ConfigTemplateEntry? Find(string id)
    {
        return GetAll().FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public static string ServiceKey(SupportedServices service) =>
        service.ToString().ToLowerInvariant();

    // Yields entries whose id is the unprefixed profile file name.
    private IEnumerable<ConfigTemplateEntry> GetServiceTemplates(SupportedServices service)
    {
        var groups = profileGroups.Get(service);
        var sizeTypes = qualitySizes
            .Get(service)
            .ToDictionary(s => s.TrashId, s => s.Type, StringComparer.OrdinalIgnoreCase);

        foreach (var profile in profiles.Get(service))
        {
            if (profile.SourceFile is not { } file)
            {
                continue;
            }

            var groupName = groups
                .FirstOrDefault(g => g.Profiles.Values.Contains(profile.TrashId))
                ?.Name;
            if (string.Equals(groupName, ExcludedProfileGroup, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new ConfigTemplateEntry(
                Path.GetFileNameWithoutExtension(file.Name),
                service,
                profile,
                QualityDefinitionType(service, profile, groupName, sizeTypes)
            );
        }
    }

    // Uses the quality size the profile names in the guide. Profiles that name none get the type
    // the retired config-templates generator inferred from the service and profile group.
    private static string QualityDefinitionType(
        SupportedServices service,
        QualityProfileResource profile,
        string? groupName,
        Dictionary<string, string> sizeTypes
    )
    {
        var namedType = profile
            .TrashQualityDefinition.Values.Select(id => sizeTypes.GetValueOrDefault(id))
            .FirstOrDefault(type => type is not null);
        if (namedType is not null)
        {
            return namedType;
        }

        var isAnime = string.Equals(groupName, "anime", StringComparison.OrdinalIgnoreCase);
        var isSqp = string.Equals(groupName, "sqp", StringComparison.OrdinalIgnoreCase);

        return service switch
        {
            SupportedServices.Sonarr => isAnime ? "anime" : "series",
            _ when isAnime => "anime",
            _ when isSqp => profile.Name.Contains("SQP-1", StringComparison.Ordinal)
                ? "sqp-streaming"
                : "sqp-uhd",
            _ => "movie",
        };
    }
}

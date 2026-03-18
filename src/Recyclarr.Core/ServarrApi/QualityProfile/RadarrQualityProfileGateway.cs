using System.Globalization;
using Recyclarr.Common.Extensions;
using Recyclarr.Servarr.QualityProfile;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.QualityProfile;

internal class RadarrQualityProfileGateway(
    ILogger log,
    RadarrApi.IQualityProfileApi profileApi,
    RadarrApi.IQualityProfileSchemaApi schemaApi,
    RadarrApi.ILanguageApi languageApi
) : IQualityProfileService
{
    private readonly Dictionary<int, RadarrApi.QualityProfileResource> _stashedProfiles = [];
    private RadarrApi.QualityProfileResource? _stashedSchema;

    public async Task<IReadOnlyList<QualityProfileData>> GetQualityProfiles(CancellationToken ct)
    {
        var dtos = await profileApi.QualityprofileGet(ct);
        foreach (var dto in dtos)
        {
            dto.ReverseItems();
            if (dto.Id is { } id)
            {
                _stashedProfiles[id] = dto;
            }
        }

        return dtos.Select(RadarrQualityProfileMapper.ToDomain).ToList();
    }

    public async Task<QualityProfileData> GetSchema(CancellationToken ct)
    {
        var dto = await schemaApi.Schema(ct);
        dto.ReverseItems();
        _stashedSchema = dto;
        return RadarrQualityProfileMapper.ToDomain(dto);
    }

    public async Task<IReadOnlyList<ProfileLanguage>> GetLanguages(CancellationToken ct)
    {
        var dtos = await languageApi.LanguageGet(ct);
        return dtos.Select(RadarrQualityProfileMapper.ToDomain).ToList();
    }

    public async Task<QualityProfileData> CreateQualityProfile(
        QualityProfileData profile,
        CancellationToken ct
    )
    {
        var dto = FromDomainForCreate(profile);
        dto.ReverseItems();
        var result = await profileApi.QualityprofilePost(dto, ct);
        result.ReverseItems();
        if (result.Id is { } resultId)
        {
            _stashedProfiles[resultId] = result;
        }
        return RadarrQualityProfileMapper.ToDomain(result);
    }

    public async Task UpdateQualityProfile(QualityProfileData profile, CancellationToken ct)
    {
        var dto = FromDomainForUpdate(profile);
        dto.ReverseItems();
        // non-null: update requires an Id (validated in FromDomainForUpdate)
        var idStr = dto.Id!.Value.ToString(CultureInfo.InvariantCulture);
        await profileApi.QualityprofilePut(idStr, dto, ct);
    }

    // Merges domain changes onto the stashed profile DTO for round-trip safety
    private RadarrApi.QualityProfileResource FromDomainForUpdate(QualityProfileData domain)
    {
        // non-null: update requires an Id, and stash is populated during GetQualityProfiles
        var stashed = _stashedProfiles[domain.Id!.Value];
        return MergeOntoDto(stashed, domain);
    }

    // Uses the stashed schema as base for new profiles (provides full quality hierarchy structure)
    private RadarrApi.QualityProfileResource FromDomainForCreate(QualityProfileData domain)
    {
        var baseDto = _stashedSchema ?? new RadarrApi.QualityProfileResource();
        return MergeOntoDto(baseDto, domain);
    }

    private RadarrApi.QualityProfileResource MergeOntoDto(
        RadarrApi.QualityProfileResource baseDto,
        QualityProfileData domain
    )
    {
        var qualityIndex = BuildQualityItemIndex(baseDto.Items ?? []);
        // Filter null Format keys: API resources always have a Format Id
        var formatIndex = (baseDto.FormatItems ?? [])
            .Where(f => f.Format is not null)
            .ToDictionary(f => f.Format!.Value);

        baseDto.Id = domain.Id ?? baseDto.Id;
        baseDto.Name = domain.Name;
        baseDto.UpgradeAllowed = domain.UpgradeAllowed ?? baseDto.UpgradeAllowed;
        baseDto.MinFormatScore = domain.MinFormatScore ?? baseDto.MinFormatScore;
        baseDto.MinUpgradeFormatScore =
            domain.MinUpgradeFormatScore ?? baseDto.MinUpgradeFormatScore;
        baseDto.Cutoff = domain.Cutoff ?? baseDto.Cutoff;
        baseDto.CutoffFormatScore = domain.CutoffFormatScore ?? baseDto.CutoffFormatScore;
        baseDto.FormatItems = domain
            .FormatItems.Select(f => MergeFormatItem(f, formatIndex))
            .ToList();
        baseDto.Items = domain.Items.Select(i => MergeItem(i, qualityIndex)).ToList();

        // Radarr-specific: language field on the profile resource
        if (domain.Language is not null)
        {
            baseDto.Language = new RadarrApi.Language
            {
                Id = domain.Language.Id,
                Name = domain.Language.Name,
            };
        }

        return baseDto;
    }

    private static RadarrApi.ProfileFormatItemResource MergeFormatItem(
        QualityProfileFormatItem domain,
        Dictionary<int, RadarrApi.ProfileFormatItemResource> index
    )
    {
        if (index.TryGetValue(domain.FormatId, out var stashed))
        {
            stashed.Score = domain.Score;
            return stashed;
        }

        return new RadarrApi.ProfileFormatItemResource
        {
            Format = domain.FormatId,
            Name = domain.Name,
            Score = domain.Score,
        };
    }

    private RadarrApi.QualityProfileQualityItemResource MergeItem(
        QualityProfileItem domain,
        Dictionary<QualityItemKey, RadarrApi.QualityProfileQualityItemResource> index
    )
    {
        var key = QualityItemKey.From(domain);
        if (index.TryGetValue(key, out var stashed))
        {
            if (key.IsGroup && stashed.Name != domain.Name)
            {
                log.Debug(
                    "MergeItem: Group Id {GroupId} stash hit with name mismatch: "
                        + "stashed '{StashedName}' vs domain '{DomainName}'",
                    key.Id,
                    stashed.Name,
                    domain.Name
                );
            }

            stashed.Id = domain.Id ?? stashed.Id;
            stashed.Name = domain.Name ?? stashed.Name;
            stashed.Allowed = domain.Allowed ?? stashed.Allowed;
            stashed.Items = domain.Items.Select(i => MergeItem(i, index)).ToList();
            return stashed;
        }

        return new RadarrApi.QualityProfileQualityItemResource
        {
            Id = domain.Id ?? 0,
            Name = domain.Name ?? "",
            Allowed = domain.Allowed ?? false,
            Quality = domain.Quality is not null
                ? new RadarrApi.Quality
                {
                    Id = domain.Quality.Id ?? 0,
                    Name = domain.Quality.Name ?? "",
                }
                : null,
            Items = domain.Items.Select(i => MergeItem(i, index)).ToList(),
        };
    }

    // Builds a flat index of all quality items for stash lookup.
    // Qualities are keyed by Quality.Id; groups are keyed by their own Id.
    private static Dictionary<
        QualityItemKey,
        RadarrApi.QualityProfileQualityItemResource
    > BuildQualityItemIndex(IEnumerable<RadarrApi.QualityProfileQualityItemResource> items)
    {
        var index = new Dictionary<QualityItemKey, RadarrApi.QualityProfileQualityItemResource>();
        foreach (var item in items.Flatten(x => x.Items ?? []))
        {
            var key = QualityItemKey.From(item);
            index.TryAdd(key, item);
        }

        return index;
    }
}

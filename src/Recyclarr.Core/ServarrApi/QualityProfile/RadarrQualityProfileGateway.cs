using Recyclarr.Common.Extensions;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.ServarrApi.QualityProfile;

internal class RadarrQualityProfileGateway(IQualityProfileApiService api) : IQualityProfileService
{
    private readonly Dictionary<int, QualityProfileDto> _stashedProfiles = [];
    private QualityProfileDto? _stashedSchema;

    public async Task<IReadOnlyList<QualityProfileData>> GetQualityProfiles(CancellationToken ct)
    {
        var dtos = await api.GetQualityProfiles(ct);
        foreach (var dto in dtos.Where(d => d.Id is not null))
        {
            _stashedProfiles[dto.Id!.Value] = dto;
        }

        return dtos.Select(ToDomain).ToList();
    }

    public async Task<QualityProfileData> GetSchema(CancellationToken ct)
    {
        var dto = await api.GetSchema(ct);
        _stashedSchema = dto;
        return ToDomain(dto);
    }

    public async Task<IReadOnlyList<ProfileLanguage>> GetLanguages(CancellationToken ct)
    {
        var dtos = await api.GetLanguages(ct);
        return dtos.Select(LanguageToDomain).ToList();
    }

    public async Task<QualityProfileData> CreateQualityProfile(
        QualityProfileData profile,
        CancellationToken ct
    )
    {
        var dto = FromDomainForCreate(profile);
        var result = await api.CreateQualityProfile(dto, ct);
        if (result.Id is not null)
        {
            _stashedProfiles[result.Id.Value] = result;
        }

        return ToDomain(result);
    }

    public async Task UpdateQualityProfile(QualityProfileData profile, CancellationToken ct)
    {
        var dto = FromDomainForUpdate(profile);
        await api.UpdateQualityProfile(dto, ct);
    }

    private static QualityProfileData ToDomain(QualityProfileDto dto)
    {
        return new QualityProfileData
        {
            Id = dto.Id,
            Name = dto.Name,
            UpgradeAllowed = dto.UpgradeAllowed,
            MinFormatScore = dto.MinFormatScore,
            MinUpgradeFormatScore = dto.MinUpgradeFormatScore,
            Cutoff = dto.Cutoff,
            CutoffFormatScore = dto.CutoffFormatScore,
            FormatItems = dto.FormatItems.Select(FormatItemToDomain).ToList(),
            Items = dto.Items.Select(ItemToDomain).ToList(),
            Language = dto.Language is not null ? LanguageToDomain(dto.Language) : null,
        };
    }

    private static QualityProfileFormatItem FormatItemToDomain(ProfileFormatItemDto dto)
    {
        return new QualityProfileFormatItem
        {
            FormatId = dto.Format,
            Name = dto.Name,
            Score = dto.Score,
        };
    }

    private static QualityProfileItem ItemToDomain(ProfileItemDto dto)
    {
        return new QualityProfileItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Allowed = dto.Allowed,
            Quality = dto.Quality is not null
                ? new QualityProfileItemQuality { Id = dto.Quality.Id, Name = dto.Quality.Name }
                : null,
            Items = dto.Items.Select(ItemToDomain).ToList(),
        };
    }

    private static ProfileLanguage LanguageToDomain(ProfileLanguageDto dto)
    {
        return new ProfileLanguage { Id = dto.Id, Name = dto.Name };
    }

    // Merges domain changes onto the stashed profile DTO for round-trip safety
    private QualityProfileDto FromDomainForUpdate(QualityProfileData domain)
    {
        // non-null: update requires an Id, and stash is populated during GetQualityProfiles
        var stashed = _stashedProfiles[domain.Id!.Value];
        return MergeOntoDto(stashed, domain);
    }

    // Uses the stashed schema as base for new profiles (provides full quality hierarchy structure)
    private QualityProfileDto FromDomainForCreate(QualityProfileData domain)
    {
        var baseDto = _stashedSchema ?? new QualityProfileDto();
        return MergeOntoDto(baseDto, domain);
    }

    private static QualityProfileDto MergeOntoDto(
        QualityProfileDto baseDto,
        QualityProfileData domain
    )
    {
        var qualityIndex = BuildQualityItemIndex(baseDto.Items);
        var formatIndex = baseDto.FormatItems.ToDictionary(f => f.Format);

        return baseDto with
        {
            Id = domain.Id,
            Name = domain.Name,
            UpgradeAllowed = domain.UpgradeAllowed,
            MinFormatScore = domain.MinFormatScore,
            MinUpgradeFormatScore = domain.MinUpgradeFormatScore,
            Cutoff = domain.Cutoff,
            CutoffFormatScore = domain.CutoffFormatScore,
            FormatItems = domain.FormatItems.Select(f => MergeFormatItem(f, formatIndex)).ToList(),
            Items = domain.Items.Select(i => MergeItem(i, qualityIndex)).ToList(),
            Language = domain.Language is not null
                ? new ProfileLanguageDto { Id = domain.Language.Id, Name = domain.Language.Name }
                : baseDto.Language,
        };
    }

    private static ProfileFormatItemDto MergeFormatItem(
        QualityProfileFormatItem domain,
        Dictionary<int, ProfileFormatItemDto> index
    )
    {
        if (index.TryGetValue(domain.FormatId, out var stashed))
        {
            return stashed with { Score = domain.Score };
        }

        return new ProfileFormatItemDto
        {
            Format = domain.FormatId,
            Name = domain.Name,
            Score = domain.Score,
        };
    }

    private static ProfileItemDto MergeItem(
        QualityProfileItem domain,
        Dictionary<QualityItemKey, ProfileItemDto> index
    )
    {
        var key = QualityItemKey.From(domain);
        if (index.TryGetValue(key, out var stashed))
        {
            return stashed with
            {
                Id = domain.Id,
                Allowed = domain.Allowed,
                Items = domain.Items.Select(i => MergeItem(i, index)).ToList(),
            };
        }

        return new ProfileItemDto
        {
            Id = domain.Id,
            Name = domain.Name,
            Allowed = domain.Allowed,
            Quality = domain.Quality is not null
                ? new ProfileItemQualityDto { Id = domain.Quality.Id, Name = domain.Quality.Name }
                : null,
            Items = domain.Items.Select(i => MergeItem(i, index)).ToList(),
        };
    }

    // Builds a flat index of all quality items for stash lookup.
    // Qualities are keyed by Quality.Id; groups are keyed by their own Id.
    private static Dictionary<QualityItemKey, ProfileItemDto> BuildQualityItemIndex(
        IEnumerable<ProfileItemDto> items
    )
    {
        var index = new Dictionary<QualityItemKey, ProfileItemDto>();
        foreach (var item in items.Flatten(x => x.Items))
        {
            var key = QualityItemKey.From(item);
            index.TryAdd(key, item);
        }

        return index;
    }
}

using System.Globalization;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.QualityProfile.Cache;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class UpdatedProfileBuilder
{
    private readonly ILogger _log;
    private readonly QualityProfileCache _cache;
    private readonly QualityProfileTransactionData _transactions;
    private readonly Dictionary<int, QualityProfileDto> _serviceDtosById;
    private readonly ILookup<string, QualityProfileDto> _serviceDtosByName;
    private readonly QualityProfileDto _schema;
    private readonly IReadOnlyList<ProfileLanguageDto> _languages;
    private readonly List<UpdatedQualityProfile> _updatedProfiles = [];

    public UpdatedProfileBuilder(
        ILogger log,
        QualityProfileServiceData serviceData,
        QualityProfileCache cache,
        QualityProfileTransactionData transactions
    )
    {
        _log = log;
        _cache = cache;
        _transactions = transactions;
        _schema = serviceData.Schema;
        _languages = serviceData.Languages;

        _serviceDtosById = serviceData
            .Profiles.Where(p => p.Id.HasValue)
            .ToDictionary(p => p.Id!.Value);
        _serviceDtosByName = serviceData.Profiles.ToLookup(
            p => p.Name,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public List<UpdatedQualityProfile> BuildFrom(IEnumerable<PlannedQualityProfile> plannedProfiles)
    {
        foreach (var planned in plannedProfiles)
        {
            if (planned.Resource is not null)
            {
                ProcessGuideBackedProfile(planned);
            }
            else
            {
                ProcessUserDefinedProfile(planned);
            }
        }

        return _updatedProfiles;
    }

    private void ProcessGuideBackedProfile(PlannedQualityProfile planned)
    {
        var trashId = planned.Resource!.TrashId;
        var cachedId = _cache.FindIdByTrashId(trashId);

        _log.Debug(
            "Process transaction for guide QP {TrashId} ({Name}), cached ID: {CachedId}",
            trashId,
            planned.Name,
            cachedId?.ToString(CultureInfo.InvariantCulture) ?? "none"
        );

        if (cachedId.HasValue)
        {
            ProcessCachedProfile(planned, cachedId.Value);
        }
        else
        {
            ProcessUncachedProfile(planned);
        }
    }

    private void ProcessCachedProfile(PlannedQualityProfile planned, int cachedId)
    {
        if (_serviceDtosById.TryGetValue(cachedId, out var serviceDto))
        {
            if (!serviceDto.Name.EqualsIgnoreCase(planned.Name))
            {
                _log.Debug(
                    "QP {TrashId} will be renamed from '{ServiceName}' to '{GuideName}'",
                    planned.Resource!.TrashId,
                    serviceDto.Name,
                    planned.Name
                );
            }

            var missingQualities = FixupMissingQualities(serviceDto);
            AddUpdatedProfile(planned, serviceDto, QualityProfileUpdateReason.Changed);
            _updatedProfiles[^1].MissingQualities = missingQualities;
        }
        else
        {
            _log.Debug(
                "Cached service ID {CachedId} for QP {TrashId} no longer exists in service",
                cachedId,
                planned.Resource!.TrashId
            );

            ProcessNameCollision(planned);
        }
    }

    private void ProcessUncachedProfile(PlannedQualityProfile planned)
    {
        ProcessNameCollision(planned);
    }

    private void ProcessNameCollision(PlannedQualityProfile planned)
    {
        var nameMatches = _serviceDtosByName[planned.Name].ToList();

        switch (nameMatches.Count)
        {
            case 0:
                if (planned.ShouldCreate)
                {
                    AddUpdatedProfile(planned, _schema, QualityProfileUpdateReason.New);
                }
                else
                {
                    _transactions.NonExistentProfiles.Add(planned.Config.Name);
                }
                break;

            case 1:
                if (planned.Resource is not null)
                {
                    _transactions.ConflictingProfiles.Add(
                        new ConflictingQualityProfile(planned, nameMatches[0].Id!.Value)
                    );
                }
                else
                {
                    var serviceDto = nameMatches[0];
                    var missingQualities = FixupMissingQualities(serviceDto);
                    AddUpdatedProfile(planned, serviceDto, QualityProfileUpdateReason.Changed);
                    _updatedProfiles[^1].MissingQualities = missingQualities;
                }
                break;

            default:
                _transactions.AmbiguousProfiles.Add(
                    new AmbiguousQualityProfile(
                        planned,
                        nameMatches.Select(p => (p.Name, p.Id!.Value)).ToList()
                    )
                );
                break;
        }
    }

    private void ProcessUserDefinedProfile(PlannedQualityProfile planned)
    {
        var nameMatches = _serviceDtosByName[planned.Name].ToList();

        switch (nameMatches.Count)
        {
            case 0:
                if (planned.ShouldCreate)
                {
                    AddUpdatedProfile(planned, _schema, QualityProfileUpdateReason.New);
                }
                else
                {
                    _transactions.NonExistentProfiles.Add(planned.Config.Name);
                }
                break;

            case 1:
                var serviceDto = nameMatches[0];
                var missingQualities = FixupMissingQualities(serviceDto);
                AddUpdatedProfile(planned, serviceDto, QualityProfileUpdateReason.Changed);
                _updatedProfiles[^1].MissingQualities = missingQualities;
                break;

            default:
                _transactions.AmbiguousProfiles.Add(
                    new AmbiguousQualityProfile(
                        planned,
                        nameMatches.Select(p => (p.Name, p.Id!.Value)).ToList()
                    )
                );
                break;
        }
    }

    private void AddUpdatedProfile(
        PlannedQualityProfile planned,
        QualityProfileDto dto,
        QualityProfileUpdateReason reason
    )
    {
        var organizer = new QualityItemOrganizer();
        _updatedProfiles.Add(
            new UpdatedQualityProfile
            {
                ProfileConfig = planned,
                ProfileDto = dto,
                UpdateReason = reason,
                Languages = _languages,
                UpdatedQualities = organizer.OrganizeItems(dto, planned.Config),
            }
        );
    }

    private List<string> FixupMissingQualities(QualityProfileDto dto)
    {
        // There's a rare bug in Sonarr & Radarr where core qualities get lost in existing profiles.
        // See: https://github.com/Radarr/Radarr/issues/9738
        var missingQualities = _schema
            .Items.FlattenQualities()
            .LeftOuterHashJoin(dto.Items.FlattenQualities(), l => l.Quality!.Id, r => r.Quality!.Id)
            .Where(x => x.Right is null)
            .Select(x => x.Left)
            .ToList();

        dto.Items = dto.Items.Concat(missingQualities).ToList();
        return missingQualities.Select(x => x.Quality!.Name ?? $"(id: {x.Quality.Id})").ToList();
    }
}

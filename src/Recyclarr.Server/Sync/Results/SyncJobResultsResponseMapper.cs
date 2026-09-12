using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Sync.Results;

internal static class SyncJobResultsResponseMapper
{
    public static SyncJobResultsResponse Map(SyncJob job)
    {
        var result =
            job.Result
            ?? throw new InvalidOperationException("A terminal sync job must have a result");

        return new SyncJobResultsResponse(
            job.Id.Value,
            ResultValueMapper.MapCompletionStatus(result.Status),
            result.Instances.Select(MapInstance).ToList()
        )
        {
            Fault = result.Fault is null ? null : new SyncFaultResponse(result.Fault.Reference),
        };
    }

    private static SyncInstanceResultsResponse MapInstance(SyncInstanceResult instance) =>
        instance.ServiceType switch
        {
            SupportedServices.Sonarr => new SonarrInstanceResultsResponse(
                instance.InstanceName,
                ResultValueMapper.MapCompletionStatus(instance.Status),
                MapSonarrPipelines(instance.Pipelines)
            )
            {
                Failure = MapFailure(instance.Failure),
            },
            SupportedServices.Radarr => new RadarrInstanceResultsResponse(
                instance.InstanceName,
                ResultValueMapper.MapCompletionStatus(instance.Status),
                MapRadarrPipelines(instance.Pipelines)
            )
            {
                Failure = MapFailure(instance.Failure),
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(instance),
                instance.ServiceType,
                null
            ),
        };

    private static SonarrPipelinesResponse MapSonarrPipelines(
        IReadOnlyList<PipelineResult> pipelines
    )
    {
        EnsureKnownPipelines(pipelines, typeof(SonarrNamingPipelineResult));

        return new SonarrPipelinesResponse
        {
            CustomFormats = pipelines
                .OfType<CustomFormatPipelineResult>()
                .Select(CustomFormatResponseMapper.ToResponse)
                .SingleOrDefault(),
            QualityProfiles = pipelines
                .OfType<QualityProfilePipelineResult>()
                .Select(QualityProfileResponseMapper.ToResponse)
                .SingleOrDefault(),
            QualitySizes = pipelines
                .OfType<QualitySizePipelineResult>()
                .Select(QualitySizeResponseMapper.ToResponse)
                .SingleOrDefault(),
            Naming = pipelines
                .OfType<SonarrNamingPipelineResult>()
                .Select(NamingResponseMapper.ToResponse)
                .SingleOrDefault(),
            MediaManagement = pipelines
                .OfType<MediaManagementPipelineResult>()
                .Select(MediaManagementResponseMapper.ToResponse)
                .SingleOrDefault(),
        };
    }

    private static RadarrPipelinesResponse MapRadarrPipelines(
        IReadOnlyList<PipelineResult> pipelines
    )
    {
        EnsureKnownPipelines(pipelines, typeof(RadarrNamingPipelineResult));

        return new RadarrPipelinesResponse
        {
            CustomFormats = pipelines
                .OfType<CustomFormatPipelineResult>()
                .Select(CustomFormatResponseMapper.ToResponse)
                .SingleOrDefault(),
            QualityProfiles = pipelines
                .OfType<QualityProfilePipelineResult>()
                .Select(QualityProfileResponseMapper.ToResponse)
                .SingleOrDefault(),
            QualitySizes = pipelines
                .OfType<QualitySizePipelineResult>()
                .Select(QualitySizeResponseMapper.ToResponse)
                .SingleOrDefault(),
            Naming = pipelines
                .OfType<RadarrNamingPipelineResult>()
                .Select(NamingResponseMapper.ToResponse)
                .SingleOrDefault(),
            MediaManagement = pipelines
                .OfType<MediaManagementPipelineResult>()
                .Select(MediaManagementResponseMapper.ToResponse)
                .SingleOrDefault(),
        };
    }

    private static void EnsureKnownPipelines(
        IReadOnlyList<PipelineResult> pipelines,
        Type namingType
    )
    {
        var unknown = pipelines.FirstOrDefault(x =>
            x is not CustomFormatPipelineResult
            && x is not QualityProfilePipelineResult
            && x is not QualitySizePipelineResult
            && x is not MediaManagementPipelineResult
            && x.GetType() != namingType
        );

        if (unknown is not null)
        {
            throw new InvalidOperationException(
                $"Unsupported pipeline result type: {unknown.GetType().FullName}"
            );
        }
    }

    private static InstanceFailureCategory? MapFailure(OperationalFailure? failure) =>
        failure switch
        {
            null => null,
            ServiceUnavailableFailure => InstanceFailureCategory.ServiceUnavailable,
            ServiceUnauthenticatedFailure => InstanceFailureCategory.ServiceUnauthenticated,
            ServiceUnauthorizedFailure => InstanceFailureCategory.ServiceUnauthorized,
            ServiceRateLimitedFailure => InstanceFailureCategory.ServiceRateLimited,
            ServiceIncompatibleFailure => InstanceFailureCategory.ServiceIncompatible,
            SyncStateUnavailableFailure => InstanceFailureCategory.SyncStateUnavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null),
        };
}

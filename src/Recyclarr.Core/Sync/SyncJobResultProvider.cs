using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;

namespace Recyclarr.Sync;

internal class SyncJobResultProvider(IJobStorage storage) : ISyncJobResults
{
    public SyncInstanceResult GetInstanceResult(JobId jobId, string instanceName)
    {
        var naming = storage.Retrieve(jobId, instanceName, PipelineType.MediaNaming);

        return new SyncInstanceResult
        {
            CustomFormats = MapCustomFormats(
                storage.Retrieve(jobId, instanceName, PipelineType.CustomFormat)
            ),
            QualityProfiles = MapQualityProfiles(
                storage.Retrieve(jobId, instanceName, PipelineType.QualityProfile)
            ),
            QualitySizes = MapQualitySizes(
                storage.Retrieve(jobId, instanceName, PipelineType.QualitySize)
            ),
            SonarrNaming = naming is SonarrNamingComputeResult s
                ? new SonarrNamingSyncResult(s.Current, s.Desired)
                : null,
            RadarrNaming = naming is RadarrNamingComputeResult r
                ? new RadarrNamingSyncResult(r.Current, r.Desired)
                : null,
            MediaManagement = MapMediaManagement(
                storage.Retrieve(jobId, instanceName, PipelineType.MediaManagement)
            ),
        };
    }

    private static CustomFormatSyncResult? MapCustomFormats(object? result) =>
        result is CustomFormatComputeResult cf
            ? new CustomFormatSyncResult(cf.Transactions, cf.SourceInfo)
            : null;

    private static QualityProfileSyncResult? MapQualityProfiles(object? result) =>
        result is QualityProfileComputeResult qp
            ? new QualityProfileSyncResult(qp.Transactions)
            : null;

    private static QualitySizeSyncResult? MapQualitySizes(object? result) =>
        result is QualitySizeComputeResult qs
            ? new QualitySizeSyncResult(qs.Items, qs.Limits, qs.QualityDefinitionType)
            : null;

    private static MediaManagementSyncResult? MapMediaManagement(object? result) =>
        result is MediaManagementComputeResult mm
            ? new MediaManagementSyncResult(mm.Current, mm.Desired)
            : null;
}

using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.CustomFormat.Models;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Sync;

public record SyncInstanceResult
{
    public CustomFormatSyncResult? CustomFormats { get; init; }
    public QualityProfileSyncResult? QualityProfiles { get; init; }
    public QualitySizeSyncResult? QualitySizes { get; init; }
    public SonarrNamingSyncResult? SonarrNaming { get; init; }
    public RadarrNamingSyncResult? RadarrNaming { get; init; }
    public MediaManagementSyncResult? MediaManagement { get; init; }
}

public record CustomFormatSyncResult(
    CustomFormatTransactionData Transactions,
    IReadOnlyDictionary<string, CustomFormatSourceInfo> SourceInfo
);

public record QualityProfileSyncResult(QualityProfileTransactionData Transactions);

public record QualitySizeSyncResult(
    IReadOnlyCollection<UpdatedQualityItem> Items,
    QualityItemLimits Limits,
    string QualityDefinitionType
);

public record SonarrNamingSyncResult(SonarrNamingData Current, SonarrNamingData Desired);

public record RadarrNamingSyncResult(RadarrNamingData Current, RadarrNamingData Desired);

public record MediaManagementSyncResult(MediaManagementData Current, MediaManagementData Desired);

using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Pipelines.MediaNaming.Sonarr;

internal record SonarrNamingComputeResult(SonarrNamingData Current, SonarrNamingData Desired);

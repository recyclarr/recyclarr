using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Pipelines.MediaNaming.Radarr;

internal record RadarrNamingComputeResult(RadarrNamingData Current, RadarrNamingData Desired);

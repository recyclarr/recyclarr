using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Resources;

/// <summary>
/// Guide data and the configuration resolved against it, published together by one refresh.
/// Configuration belongs here because template includes come from resource providers.
/// </summary>
internal sealed record ResourceSnapshot(GuideIndex Guide, ServerConfiguration Configuration);

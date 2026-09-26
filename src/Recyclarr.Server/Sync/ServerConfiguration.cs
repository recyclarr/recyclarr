using Recyclarr.Config.Models;

namespace Recyclarr.Server.Sync;

// The instances loaded and validated once at startup. Configuration is fixed for the life of the
// process; changes require a restart (ADR-019).
internal sealed record ServerConfiguration(IReadOnlyList<IServiceConfiguration> Instances);

using Recyclarr.Config.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Sync;

// The instances loaded and validated once at startup. Configuration is fixed for the life of the
// process; changes require a restart (ADR-019).
internal sealed record ServerConfiguration(IReadOnlyList<IServiceConfiguration> Instances)
{
    public IReadOnlyList<string> InstanceNames => [.. Instances.Select(x => x.InstanceName)];

    public IReadOnlyList<string> FindUnknown(IReadOnlyCollection<string> names) =>
        [.. names.Where(name => !InstanceNames.Contains(name, StringComparer.OrdinalIgnoreCase))];

    // An empty name list selects every instance of the requested service.
    public IReadOnlyList<IServiceConfiguration> Select(
        SupportedServices? service,
        IReadOnlyCollection<string> names
    ) =>
        [
            .. Instances.Where(x =>
                (service is null || x.ServiceType == service)
                && (
                    names.Count == 0
                    || names.Contains(x.InstanceName, StringComparer.OrdinalIgnoreCase)
                )
            ),
        ];
}

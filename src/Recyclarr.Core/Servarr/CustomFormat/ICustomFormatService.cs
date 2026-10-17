using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Servarr.CustomFormat;

// Uses the guide resource type as the domain type. CF sync is one-way (the guide is the complete
// desired state), so gateways map generated DTOs to it without stashing.
public interface ICustomFormatService
{
    Task<IReadOnlyList<CustomFormatResource>> GetCustomFormats(CancellationToken ct);
    Task<CustomFormatResource?> CreateCustomFormat(CustomFormatResource cf, CancellationToken ct);
    Task UpdateCustomFormat(CustomFormatResource cf, CancellationToken ct);
    Task DeleteCustomFormat(int customFormatId, CancellationToken ct);
}

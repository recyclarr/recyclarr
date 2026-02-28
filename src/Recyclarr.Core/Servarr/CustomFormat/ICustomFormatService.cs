using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Servarr.CustomFormat;

// Passthrough exception: uses CustomFormatResource directly as the domain type
// instead of a separate domain/DTO split. See ADR-007 for rationale.
public interface ICustomFormatService
{
    Task<IReadOnlyList<CustomFormatResource>> GetCustomFormats(CancellationToken ct);
    Task<CustomFormatResource?> CreateCustomFormat(CustomFormatResource cf, CancellationToken ct);
    Task UpdateCustomFormat(CustomFormatResource cf, CancellationToken ct);
    Task DeleteCustomFormat(int customFormatId, CancellationToken ct);
}

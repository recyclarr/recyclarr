using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.ServarrApi.CustomFormat;

public interface ICustomFormatApiService
{
    Task<IList<CustomFormatResource>> GetCustomFormats(CancellationToken ct);
    Task<CustomFormatResource?> CreateCustomFormat(CustomFormatResource cf, CancellationToken ct);
    Task UpdateCustomFormat(CustomFormatResource cf, CancellationToken ct);
    Task DeleteCustomFormat(int customFormatId, CancellationToken ct);
}

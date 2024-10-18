using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ServarrApi.CustomFormat;

public interface ICustomFormatApiService
{
    Task<IList<CustomFormatData>> GetCustomFormats(CancellationToken ct);
    Task<CustomFormatData?> CreateCustomFormat(CustomFormatData cf, CancellationToken ct);
    Task UpdateCustomFormat(CustomFormatData cf, CancellationToken ct);
    Task DeleteCustomFormat(int customFormatId, CancellationToken ct);
}

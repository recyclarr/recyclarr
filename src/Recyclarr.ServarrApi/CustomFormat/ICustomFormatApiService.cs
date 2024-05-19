using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ServarrApi.CustomFormat;

public interface ICustomFormatApiService
{
    Task<IList<CustomFormatData>> GetCustomFormats();
    Task<CustomFormatData?> CreateCustomFormat(CustomFormatData cf);
    Task UpdateCustomFormat(CustomFormatData cf);
    Task DeleteCustomFormat(int customFormatId, CancellationToken cancellationToken = default);
}

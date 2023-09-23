using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ServarrApi.Services;

public interface ICustomFormatService
{
    Task<IList<CustomFormatData>> GetCustomFormats(IServiceConfiguration config);
    Task<CustomFormatData?> CreateCustomFormat(IServiceConfiguration config, CustomFormatData cf);
    Task UpdateCustomFormat(IServiceConfiguration config, CustomFormatData cf);

    Task DeleteCustomFormat(
        IServiceConfiguration config,
        int customFormatId,
        CancellationToken cancellationToken = default);
}

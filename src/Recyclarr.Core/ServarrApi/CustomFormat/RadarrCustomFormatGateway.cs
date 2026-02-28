using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;

namespace Recyclarr.ServarrApi.CustomFormat;

internal class RadarrCustomFormatGateway(ICustomFormatApiService api) : ICustomFormatService
{
    public async Task<IReadOnlyList<CustomFormatResource>> GetCustomFormats(CancellationToken ct)
    {
        var result = await api.GetCustomFormats(ct);
        return result.ToList();
    }

    public async Task<CustomFormatResource?> CreateCustomFormat(
        CustomFormatResource cf,
        CancellationToken ct
    )
    {
        return await api.CreateCustomFormat(cf, ct);
    }

    public async Task UpdateCustomFormat(CustomFormatResource cf, CancellationToken ct)
    {
        await api.UpdateCustomFormat(cf, ct);
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken ct)
    {
        await api.DeleteCustomFormat(customFormatId, ct);
    }
}

using Flurl.Http;

namespace Recyclarr.ServarrApi.QualityDefinition;

internal class QualityDefinitionApiService(IServarrRequestBuilder service) : IQualityDefinitionApiService
{
    private IFlurlRequest Request(params object[] path)
    {
        return service.Request(["qualitydefinition", ..path]);
    }

    public async Task<IList<ServiceQualityDefinitionItem>> GetQualityDefinition(CancellationToken ct)
    {
        return await Request()
            .GetJsonAsync<List<ServiceQualityDefinitionItem>>(cancellationToken: ct);
    }

    public async Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IList<ServiceQualityDefinitionItem> newQuality,
        CancellationToken ct)
    {
        return await Request("update")
            .PutJsonAsync(newQuality, cancellationToken: ct)
            .ReceiveJson<List<ServiceQualityDefinitionItem>>();
    }
}

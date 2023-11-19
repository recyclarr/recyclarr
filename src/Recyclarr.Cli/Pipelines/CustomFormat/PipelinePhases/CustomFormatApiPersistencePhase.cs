using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatApiPersistencePhase(ICustomFormatApiService api)
{
    public async Task Execute(IServiceConfiguration config, CustomFormatTransactionData transactions)
    {
        foreach (var cf in transactions.NewCustomFormats)
        {
            var response = await api.CreateCustomFormat(config, cf);
            if (response is not null)
            {
                cf.Id = response.Id;
            }
        }

        foreach (var dto in transactions.UpdatedCustomFormats)
        {
            await api.UpdateCustomFormat(config, dto);
        }

        foreach (var map in transactions.DeletedCustomFormats)
        {
            await api.DeleteCustomFormat(config, map.CustomFormatId);
        }
    }
}

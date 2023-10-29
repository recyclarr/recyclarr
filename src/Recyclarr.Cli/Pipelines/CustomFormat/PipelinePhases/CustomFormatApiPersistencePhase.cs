using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatApiPersistencePhase(ICustomFormatApiService api, ICustomFormatCachePersister cachePersister)
    : IApiPersistencePipelinePhase<CustomFormatPipelineContext>
{
    public async Task Execute(CustomFormatPipelineContext context, IServiceConfiguration config)
    {
        var transactions = context.TransactionOutput;

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

        context.Cache.Update(transactions);
        cachePersister.Save(config, context.Cache);
    }
}

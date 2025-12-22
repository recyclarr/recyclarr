using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatApiPersistencePhase(
    ICustomFormatApiService api,
    ICachePersister<CustomFormatCacheObject> cachePersister,
    CustomFormatTransactionLogger cfLogger
) : IPipelinePhase<CustomFormatPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        CustomFormatPipelineContext context,
        CancellationToken ct
    )
    {
        var hasBlockingErrors = cfLogger.LogTransactions(context);
        if (hasBlockingErrors)
        {
            return PipelineFlow.Terminate;
        }

        var transactions = context.TransactionOutput;

        foreach (var cf in transactions.NewCustomFormats)
        {
            var response = await api.CreateCustomFormat(cf, ct);
            if (response is not null)
            {
                cf.Id = response.Id;
            }
        }

        foreach (var dto in transactions.UpdatedCustomFormats)
        {
            await api.UpdateCustomFormat(dto, ct);
        }

        foreach (var map in transactions.DeletedCustomFormats)
        {
            await api.DeleteCustomFormat(map.ServiceId, ct);
        }

        context.Cache.Update(context);
        cachePersister.Save(context.Cache);

        return PipelineFlow.Continue;
    }
}

using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatApiPersistencePhase(
    ICustomFormatApiService api,
    ICachePersister<CustomFormatCache> cachePersister,
    CustomFormatTransactionLogger cfLogger
) : IPipelinePhase<CustomFormatPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        CustomFormatPipelineContext context,
        CancellationToken ct
    )
    {
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
            await api.DeleteCustomFormat(map.CustomFormatId, ct);
        }

        context.Cache.Update(transactions);
        cachePersister.Save(context.Cache);

        cfLogger.LogTransactions(context);
        return PipelineFlow.Continue;
    }
}

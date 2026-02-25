using Recyclarr.Cli.Pipelines.CustomFormat.State;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatApiPersistencePhase(
    ICustomFormatApiService api,
    ICustomFormatStatePersister statePersister,
    CustomFormatTransactionLogger cfLogger,
    IServiceConfiguration config
) : IPipelinePhase<CustomFormatPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        CustomFormatPipelineContext context,
        CancellationToken ct
    )
    {
        cfLogger.LogTransactions(context);

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

        if (config.DeleteOldCustomFormats)
        {
            foreach (var map in transactions.DeletedCustomFormats)
            {
                await api.DeleteCustomFormat(map.ServiceId, ct);
            }
        }

        context.State.Update(context);
        statePersister.Save(context.State);

        return PipelineFlow.Continue;
    }
}

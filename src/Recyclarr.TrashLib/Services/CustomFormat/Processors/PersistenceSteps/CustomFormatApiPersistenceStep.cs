using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Api;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

internal class CustomFormatApiPersistenceStep : ICustomFormatApiPersistenceStep
{
    private readonly ICustomFormatService _api;

    public CustomFormatApiPersistenceStep(ICustomFormatService api)
    {
        _api = api;
    }

    public async Task Process(IServiceConfiguration config, CustomFormatTransactionData transactions)
    {
        foreach (var cf in transactions.NewCustomFormats)
        {
            await _api.CreateCustomFormat(config, cf);
        }

        foreach (var cf in transactions.UpdatedCustomFormats)
        {
            await _api.UpdateCustomFormat(config, cf);
        }

        foreach (var cfId in transactions.DeletedCustomFormatIds)
        {
            await _api.DeleteCustomFormat(config, cfId.CustomFormatId);
        }
    }
}

using TrashLib.Services.Radarr.CustomFormat.Api;

namespace TrashLib.Services.Radarr.CustomFormat.Processors.PersistenceSteps;

public interface ICustomFormatApiPersistenceStep
{
    Task Process(ICustomFormatService api, CustomFormatTransactionData transactions);
}

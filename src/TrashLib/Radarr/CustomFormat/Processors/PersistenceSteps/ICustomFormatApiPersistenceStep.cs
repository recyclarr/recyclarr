using TrashLib.Radarr.CustomFormat.Api;

namespace TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps;

public interface ICustomFormatApiPersistenceStep
{
    Task Process(ICustomFormatService api, CustomFormatTransactionData transactions);
}

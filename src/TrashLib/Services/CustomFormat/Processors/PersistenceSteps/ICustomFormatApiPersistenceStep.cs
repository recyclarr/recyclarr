using TrashLib.Services.CustomFormat.Api;

namespace TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public interface ICustomFormatApiPersistenceStep
{
    Task Process(ICustomFormatService api, CustomFormatTransactionData transactions);
}

using Recyclarr.TrashLib.Services.CustomFormat.Api;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public interface ICustomFormatApiPersistenceStep
{
    Task Process(ICustomFormatService api, CustomFormatTransactionData transactions);
}

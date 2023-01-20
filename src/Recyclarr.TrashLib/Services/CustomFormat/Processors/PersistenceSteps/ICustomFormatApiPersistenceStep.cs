using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public interface ICustomFormatApiPersistenceStep
{
    Task Process(IServiceConfiguration config, CustomFormatTransactionData transactions);
}

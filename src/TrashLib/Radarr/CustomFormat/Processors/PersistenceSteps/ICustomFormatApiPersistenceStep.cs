using System.Threading.Tasks;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Api;

namespace TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps
{
    public interface ICustomFormatApiPersistenceStep
    {
        Task Process(RadarrConfig config, ICustomFormatService api, CustomFormatTransactionData transactions);
    }
}

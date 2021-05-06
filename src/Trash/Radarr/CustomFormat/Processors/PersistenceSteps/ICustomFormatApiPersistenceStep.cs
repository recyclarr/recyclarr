using System.Threading.Tasks;
using Trash.Radarr.CustomFormat.Api;

namespace Trash.Radarr.CustomFormat.Processors.PersistenceSteps
{
    public interface ICustomFormatApiPersistenceStep
    {
        Task Process(ICustomFormatService api, CustomFormatTransactionData transactions);
    }
}

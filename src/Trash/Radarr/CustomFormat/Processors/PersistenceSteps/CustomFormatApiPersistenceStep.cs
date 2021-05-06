using System.Threading.Tasks;
using Trash.Radarr.CustomFormat.Api;

namespace Trash.Radarr.CustomFormat.Processors.PersistenceSteps
{
    public class CustomFormatApiPersistenceStep : ICustomFormatApiPersistenceStep
    {
        public async Task Process(ICustomFormatService api, CustomFormatTransactionData transactions)
        {
            foreach (var cf in transactions.NewCustomFormats)
            {
                await api.CreateCustomFormat(cf);
            }

            foreach (var cf in transactions.UpdatedCustomFormats)
            {
                await api.UpdateCustomFormat(cf);
            }

            foreach (var cfId in transactions.DeletedCustomFormatIds)
            {
                await api.DeleteCustomFormat(cfId.CustomFormatId);
            }
        }
    }
}

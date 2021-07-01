using System.Linq;
using System.Threading.Tasks;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Api;
using TrashLib.Radarr.CustomFormat.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps
{
    internal class CustomFormatApiPersistenceStep : ICustomFormatApiPersistenceStep
    {
        private readonly ICustomFormatCache _cache;

        public CustomFormatApiPersistenceStep(ICustomFormatCache cache)
        {
            _cache = cache;
        }

        public async Task Process(RadarrConfig config, ICustomFormatService api, CustomFormatTransactionData transactions)
        {
            foreach (var cf in transactions.NewCustomFormats)
            {
                var id = await api.CreateCustomFormat(cf);
                _cache.Add(id, cf);
            }

            foreach (var cf in transactions.UpdatedCustomFormats)
            {
                await api.UpdateCustomFormat(cf.Id, cf.CustomFormat);
            }

            foreach (var cfId in transactions.DeletedCustomFormatIds)
            {
                await api.DeleteCustomFormat(cfId.CustomFormatId);
                _cache.Remove(cfId);
            }
        }
    }
}

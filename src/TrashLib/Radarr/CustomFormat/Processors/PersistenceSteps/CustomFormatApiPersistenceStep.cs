using System;
using System.Threading.Tasks;
using TrashLib.Config;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Api;
using TrashLib.Radarr.CustomFormat.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps
{
    internal class CustomFormatApiPersistenceStep : ICustomFormatApiPersistenceStep
    {
        private readonly Func<IServiceConfiguration, ICustomFormatCache> _cacheFactory;

        public CustomFormatApiPersistenceStep(Func<IServiceConfiguration, ICustomFormatCache> cacheFactory)
        {
            _cacheFactory = cacheFactory;
        }

        public async Task Process(RadarrConfig config, ICustomFormatService api,
            CustomFormatTransactionData transactions)
        {
            var cache = _cacheFactory(config);

            foreach (var cf in transactions.NewCustomFormats)
            {
                var id = await api.CreateCustomFormat(cf);
                cache.Add(id, cf);
            }

            foreach (var (customFormat, id) in transactions.UpdatedCustomFormats)
            {
                await api.UpdateCustomFormat(id, customFormat);
            }

            foreach (var cfId in transactions.DeletedCustomFormatIds)
            {
                await api.DeleteCustomFormat(cfId.CustomFormatId);
                cache.Remove(cfId);
            }

            cache.Save();
        }
    }
}

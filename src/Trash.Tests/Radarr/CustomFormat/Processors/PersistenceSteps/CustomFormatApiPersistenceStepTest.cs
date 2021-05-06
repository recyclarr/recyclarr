using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using Trash.Radarr.CustomFormat.Api;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;
using Trash.Radarr.CustomFormat.Processors.PersistenceSteps;

namespace Trash.Tests.Radarr.CustomFormat.Processors.PersistenceSteps
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class CustomFormatApiPersistenceStepTest
    {
        private ProcessedCustomFormatData QuickMakeCf(string cfName, string trashId, int cfId)
        {
            return new(cfName, trashId, new JObject())
            {
                CacheEntry = new TrashIdMapping(trashId, cfName) {CustomFormatId = cfId}
            };
        }

        [Test]
        public async Task All_api_operations_behave_normally()
        {
            var transactions = new CustomFormatTransactionData();
            transactions.NewCustomFormats.Add(QuickMakeCf("cfname1", "trashid1", 1));
            transactions.UpdatedCustomFormats.Add(QuickMakeCf("cfname2", "trashid2", 2));
            transactions.UnchangedCustomFormats.Add(QuickMakeCf("cfname3", "trashid3", 3));
            transactions.DeletedCustomFormatIds.Add(new TrashIdMapping("trashid4", "cfname4") {CustomFormatId = 4});

            var api = Substitute.For<ICustomFormatService>();

            var processor = new CustomFormatApiPersistenceStep();
            await processor.Process(api, transactions);

            Received.InOrder(() =>
            {
                api.CreateCustomFormat(transactions.NewCustomFormats.First());
                api.UpdateCustomFormat(transactions.UpdatedCustomFormats.First());
                api.DeleteCustomFormat(4);
            });
        }
    }
}

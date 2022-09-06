using NSubstitute;
using NUnit.Framework;
using TrashLib.Services.CustomFormat.Api;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;
using TrashLib.Services.CustomFormat.Processors.PersistenceSteps;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.CustomFormat.Processors.PersistenceSteps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatApiPersistenceStepTest
{
    private static ProcessedCustomFormatData QuickMakeCf(string cfName, string trashId, int cfId)
    {
        return NewCf.Processed(cfName, trashId, new TrashIdMapping(trashId, cfName) {CustomFormatId = cfId});
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

using NSubstitute;
using NUnit.Framework;
using TrashLib.Radarr.CustomFormat.Api;
using TrashLib.Radarr.CustomFormat.Models.Cache;
using TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.Radarr.CustomFormat.Processors.PersistenceSteps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatApiPersistenceStepTest
{
    [Test]
    public async Task All_api_operations_behave_normally()
    {
        var transactions = new CustomFormatTransactionData();
        transactions.NewCustomFormats.Add(NewCf.Processed("cfname1", "", "trashid1", 1));
        transactions.UpdatedCustomFormats.Add(NewCf.Processed("cfname2", "", "trashid2", 2));
        transactions.UnchangedCustomFormats.Add(NewCf.Processed("cfname3", "", "trashid3", 3));
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

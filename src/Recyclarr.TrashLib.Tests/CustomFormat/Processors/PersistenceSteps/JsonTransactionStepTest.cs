using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TestLibrary.FluentAssertions;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;
using Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;
using Recyclarr.TrashLib.TestLibrary;

/* Sample Custom Format response from Radarr API
{
  "id": 1,
  "name": "test",
  "includeCustomFormatWhenRenaming": false,
  "specifications": [
    {
      "name": "asdf",
      "implementation": "ReleaseTitleSpecification",
      "implementationName": "Release Title",
      "infoLink": "https://wiki.servarr.com/Radarr_Settings#Custom_Formats_2",
      "negate": false,
      "required": false,
      "fields": [
        {
          "order": 0,
          "name": "value",
          "label": "Regular Expression",
          "value": "asdf",
          "type": "textbox",
          "advanced": false
        }
      ]
    }
  ]
}
*/

namespace Recyclarr.TrashLib.Tests.CustomFormat.Processors.PersistenceSteps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class JsonTransactionStepTest
{
    [Test, AutoMockData]
    public void Combination_of_create_update_and_unchanged_and_verify_proper_json_merging(
        JsonTransactionStep processor)
    {
        var radarrCfs = JsonConvert.DeserializeObject<List<JObject>>(@"
[{
  'id': 1,
  'name': 'user_defined',
  'specifications': [{
    'name': 'spec1',
    'negate': false,
    'fields': [{
      'name': 'value',
      'value': 'value1'
    }]
  }]
}, {
  'id': 2,
  'name': 'updated',
  'specifications': [{
    'name': 'spec2',
    'negate': false,
    'fields': [{
      'name': 'value',
      'untouchable': 'field',
      'value': 'value1'
    }]
  }]
}, {
  'id': 3,
  'name': 'no_change',
  'specifications': [{
    'name': 'spec4',
    'negate': false,
    'fields': [{
      'name': 'value',
      'value': 'value1'
    }]
  }]
}]")!;
        var guideCfData = JsonConvert.DeserializeObject<List<JObject>>(@"
[{
  'name': 'created',
  'specifications': [{
    'name': 'spec5',
    'fields': {
      'value': 'value2'
    }
  }]
}, {
  'name': 'updated_different_name',
  'specifications': [{
    'name': 'spec2',
    'negate': true,
    'new_spec_field': 'new_spec_value',
    'fields': {
      'value': 'value2',
      'new_field': 'new_value'
    }
  }, {
    'name': 'new_spec',
    'fields': {
      'value': 'value3'
    }
  }]
}, {
  'name': 'no_change',
  'specifications': [{
    'name': 'spec4',
    'negate': false,
    'fields': {
      'value': 'value1'
    }
  }]
}]")!;

        var guideCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("created", "id1", guideCfData[0]),
            NewCf.Processed("updated_different_name", "id2", guideCfData[1], new TrashIdMapping("id2", "", 2)),
            NewCf.Processed("no_change", "id3", guideCfData[2], new TrashIdMapping("id3", "", 3))
        };

        processor.Process(guideCfs, radarrCfs);

        var expectedJson = new[]
        {
            @"{
  'name': 'created',
  'specifications': [{
    'name': 'spec5',
    'fields': [{
      'name': 'value',
      'value': 'value2'
    }]
  }]
}",
            @"{
  'id': 2,
  'name': 'updated_different_name',
  'specifications': [{
    'name': 'spec2',
    'negate': true,
    'new_spec_field': 'new_spec_value',
    'fields': [{
      'name': 'value',
      'untouchable': 'field',
      'value': 'value2',
      'new_field': 'new_value'
    }]
  }, {
    'name': 'new_spec',
    'fields': [{
      'name': 'value',
      'value': 'value3'
    }]
  }]
}",
            @"{
  'id': 3,
  'name': 'no_change',
  'specifications': [{
    'name': 'spec4',
    'negate': false,
    'fields': [{
      'name': 'value',
      'value': 'value1'
    }]
  }]
}"
        };

        var expectedTransactions = new CustomFormatTransactionData();
        expectedTransactions.NewCustomFormats.Add(guideCfs[0]);
        expectedTransactions.UpdatedCustomFormats.Add(guideCfs[1]);
        expectedTransactions.UnchangedCustomFormats.Add(guideCfs[2]);
        processor.Transactions.Should().BeEquivalentTo(expectedTransactions);

        processor.Transactions.NewCustomFormats.First().Json.Should()
            .BeEquivalentTo(JObject.Parse(expectedJson[0]), op => op.Using(new JsonEquivalencyStep()));

        processor.Transactions.UpdatedCustomFormats.First().Json.Should()
            .BeEquivalentTo(JObject.Parse(expectedJson[1]), op => op.Using(new JsonEquivalencyStep()));

        processor.Transactions.UnchangedCustomFormats.First().Json.Should()
            .BeEquivalentTo(JObject.Parse(expectedJson[2]), op => op.Using(new JsonEquivalencyStep()));

        processor.Transactions.ConflictingCustomFormats.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Deletes_happen_before_updates(
        JsonTransactionStep processor)
    {
        const string radarrCfData = @"[{
  'id': 1,
  'name': 'updated',
  'specifications': [{
    'name': 'spec1',
    'fields': [{
      'name': 'value',
      'value': 'value1'
    }]
  }]
}, {
  'id': 2,
  'name': 'deleted',
  'specifications': [{
    'name': 'spec2',
    'negate': false,
    'fields': [{
      'name': 'value',
      'untouchable': 'field',
      'value': 'value1'
    }]
  }]
}]";
        var guideCfData = JObject.Parse(@"{
  'name': 'updated',
  'specifications': [{
    'name': 'spec2',
    'fields': {
      'value': 'value2'
    }
  }]
}");
        var deletedCfsInCache = new List<TrashIdMapping>
        {
            new("", "") {CustomFormatId = 2}
        };

        var guideCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("updated", "", guideCfData, new TrashIdMapping("", "") {CustomFormatId = 1})
        };

        var radarrCfs = JsonConvert.DeserializeObject<List<JObject>>(radarrCfData);

        processor.Process(guideCfs, radarrCfs!);
        processor.RecordDeletions(deletedCfsInCache, radarrCfs!);

        const string expectedJson = @"{
  'id': 1,
  'name': 'updated',
  'specifications': [{
    'name': 'spec2',
    'fields': [{
      'name': 'value',
      'value': 'value2'
    }]
  }]
}";
        var expectedTransactions = new CustomFormatTransactionData();
        expectedTransactions.DeletedCustomFormatIds.Add(new TrashIdMapping("", "", 2));
        expectedTransactions.UpdatedCustomFormats.Add(guideCfs[0]);
        processor.Transactions.Should().BeEquivalentTo(expectedTransactions);

        processor.Transactions.UpdatedCustomFormats.First().Json.Should()
            .BeEquivalentTo(JObject.Parse(expectedJson), op => op.Using(new JsonEquivalencyStep()));
    }

    [Test, AutoMockData]
    public void Only_delete_correct_cfs(
        JsonTransactionStep processor)
    {
        const string radarrCfData = @"[{
  'id': 1,
  'name': 'not_deleted',
  'specifications': [{
    'name': 'spec1',
    'negate': false,
    'fields': [{
      'name': 'value',
      'value': 'value1'
    }]
  }]
}, {
  'id': 2,
  'name': 'deleted',
  'specifications': [{
    'name': 'spec2',
    'negate': false,
    'fields': [{
      'name': 'value',
      'untouchable': 'field',
      'value': 'value1'
    }]
  }]
}]";
        var deletedCfsInCache = new List<TrashIdMapping>
        {
            new("testtrashid", "", 2),
            new("", "", 3)
        };

        var radarrCfs = JsonConvert.DeserializeObject<List<JObject>>(radarrCfData);

        processor.RecordDeletions(deletedCfsInCache, radarrCfs!);

        var expectedTransactions = new CustomFormatTransactionData();
        expectedTransactions.DeletedCustomFormatIds.Add(new TrashIdMapping("testtrashid", "", 2));
        processor.Transactions.Should().BeEquivalentTo(expectedTransactions);
    }

    [Test, AutoMockData]
    public void Conflicting_ids_detected(
        JsonTransactionStep processor)
    {
        const string serviceCfData = @"
[{
  'id': 1,
  'name': 'first'
}, {
  'id': 2,
  'name': 'second'
}]";

        var serviceCfs = JsonConvert.DeserializeObject<List<JObject>>(serviceCfData)!;

        var guideCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("first", "", new TrashIdMapping("", "first", 2))
        };

        processor.Process(guideCfs, serviceCfs);

        var expectedTransactions = new CustomFormatTransactionData();
        expectedTransactions.ConflictingCustomFormats.Add(new ConflictingCustomFormat(guideCfs[0], 1));
        processor.Transactions.Should().BeEquivalentTo(expectedTransactions);
    }

    [Test, AutoMockData]
    public void Service_cf_id_set_when_no_cache_entry(JsonTransactionStep processor)
    {
        const string serviceCfData = @"
[{
  'id': 1,
  'name': 'first'
}]";

        var serviceCfs = JsonConvert.DeserializeObject<List<JObject>>(serviceCfData)!;

        var guideCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("first", "")
        };

        processor.Process(guideCfs, serviceCfs);

        processor.Transactions.UpdatedCustomFormats.Should().BeEquivalentTo(
            new[] {NewCf.Processed("first", "", new TrashIdMapping("", "first", 1))},
            o => o.Including(x => x.CacheEntry!.CustomFormatId));
    }
}

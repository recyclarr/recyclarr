using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TestLibrary.FluentAssertions;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;
using TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps;
using TrashLib.TestLibrary;

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

namespace TrashLib.Tests.Radarr.CustomFormat.Processors.PersistenceSteps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class JsonTransactionStepTest
{
    [TestCase(1, "cf2")]
    [TestCase(2, "cf1")]
    [TestCase(null, "cf1")]
    public void Updates_using_combination_of_id_and_name(int? id, string guideCfName)
    {
        const string radarrCfData = @"{
  'id': 1,
  'name': 'cf1',
  'specifications': [{
    'name': 'spec1',
    'fields': [{
      'name': 'value',
      'value': 'value1'
    }]
  }]
}";
        var guideCfData = JObject.Parse(@"{
  'name': 'cf1',
  'specifications': [{
    'name': 'spec1',
    'new': 'valuenew',
    'fields': {
      'value': 'value2'
    }
  }]
}");
        var cacheEntry = id != null ? new TrashIdMapping("", "") {CustomFormatId = id.Value} : null;

        var guideCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed(guideCfName, "", guideCfData, cacheEntry)
        };

        var processor = new JsonTransactionStep();
        processor.Process(guideCfs, new[] {JObject.Parse(radarrCfData)});

        var expectedTransactions = new CustomFormatTransactionData();
        expectedTransactions.UpdatedCustomFormats.Add(guideCfs[0]);
        processor.Transactions.Should().BeEquivalentTo(expectedTransactions);

        const string expectedJsonData = @"{
  'id': 1,
  'name': 'cf1',
  'specifications': [{
    'name': 'spec1',
    'new': 'valuenew',
    'fields': [{
      'name': 'value',
      'value': 'value2'
    }]
  }]
}";
        processor.Transactions.UpdatedCustomFormats.First().Json.Should()
            .BeEquivalentTo(JObject.Parse(expectedJsonData), op => op.Using(new JsonEquivalencyStep()));
    }

    [Test]
    public void Combination_of_create_update_and_unchanged_and_verify_proper_json_merging()
    {
        const string radarrCfData = @"[{
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
}]";
        var guideCfData = JsonConvert.DeserializeObject<List<JObject>>(@"[{
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
}]");

        var radarrCfs = JsonConvert.DeserializeObject<List<JObject>>(radarrCfData);
        var guideCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("created", "", guideCfData![0]),
            NewCf.Processed("updated_different_name", "", guideCfData[1], new TrashIdMapping("", "", 2)),
            NewCf.Processed("no_change", "", guideCfData[2])
        };

        var processor = new JsonTransactionStep();
        processor.Process(guideCfs, radarrCfs!);

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
    }

    [Test]
    public void Deletes_happen_before_updates()
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

        var processor = new JsonTransactionStep();
        processor.Process(guideCfs, radarrCfs!);
        processor.RecordDeletions(deletedCfsInCache, radarrCfs!);

        var expectedJson = @"{
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

    [Test]
    public void Only_delete_correct_cfs()
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
            new("testtrashid", "testname") {CustomFormatId = 2},
            new("", "not_deleted") {CustomFormatId = 3}
        };

        var radarrCfs = JsonConvert.DeserializeObject<List<JObject>>(radarrCfData);

        var processor = new JsonTransactionStep();
        processor.RecordDeletions(deletedCfsInCache, radarrCfs!);

        var expectedTransactions = new CustomFormatTransactionData();
        expectedTransactions.DeletedCustomFormatIds.Add(new TrashIdMapping("testtrashid", "testname", 2));
        processor.Transactions.Should().BeEquivalentTo(expectedTransactions);
    }

    [Test]
    public void Updated_and_unchanged_custom_formats_have_cache_entry_set_when_there_is_no_cache()
    {
        const string radarrCfData = @"[{
  'id': 1,
  'name': 'updated',
  'specifications': [{
    'name': 'spec2',
    'fields': [{
      'name': 'value',
      'value': 'value1'
    }]
  }]
}, {
  'id': 2,
  'name': 'no_change',
  'specifications': [{
    'name': 'spec4',
    'negate': false,
    'fields': [{
      'name': 'value',
      'value': 'value1'
    }]
  }]
}]";
        var guideCfData = JsonConvert.DeserializeObject<List<JObject>>(@"[{
  'name': 'updated',
  'specifications': [{
    'name': 'spec2',
    'fields': {
      'value': 'value2'
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
}]");

        var radarrCfs = JsonConvert.DeserializeObject<List<JObject>>(radarrCfData);
        var guideCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("updated", "", guideCfData![0]),
            NewCf.Processed("no_change", "", guideCfData[1])
        };

        var processor = new JsonTransactionStep();
        processor.Process(guideCfs, radarrCfs!);

        processor.Transactions.UpdatedCustomFormats.First().CacheEntry.Should()
            .BeEquivalentTo(new TrashIdMapping("", "updated", 1));

        processor.Transactions.UnchangedCustomFormats.First().CacheEntry.Should()
            .BeEquivalentTo(new TrashIdMapping("", "no_change", 2));
    }
}

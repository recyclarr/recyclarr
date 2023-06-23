using Newtonsoft.Json.Linq;
using Recyclarr.Cli.Pipelines.CustomFormat.Guide;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Json;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatParserTest
{
    [Test, AutoMockData]
    public void Deserialize_works(CustomFormatParser sut)
    {
        var jsonData = @"
{
  'trash_id': '90cedc1fea7ea5d11298bebd3d1d3223',
  'trash_score': '-10000',
  'name': 'EVO (no WEBDL)',
  'includeCustomFormatWhenRenaming': false,
  'specifications': [
    {
      'name': 'EVO',
      'implementation': 'ReleaseTitleSpecification',
      'negate': false,
      'required': true,
      'fields': [{
        'value': '\\bEVO(TGX)?\\b'
      }]
    },
    {
      'name': 'WEBDL',
      'implementation': 'SourceSpecification',
      'negate': true,
      'required': true,
      'fields': {
        'value': 7
      }
    },
    {
      'name': 'WEBRIP',
      'implementation': 'SourceSpecification',
      'negate': true,
      'required': true,
      'fields': {
        'value': 8
      }
    }
  ]
}";
        var result = sut.ParseCustomFormatData(jsonData, "file.json");

        result.Should().BeEquivalentTo(new CustomFormatData
        {
            FileName = "file.json",
            TrashId = "90cedc1fea7ea5d11298bebd3d1d3223",
            TrashScore = -10000,
            Name = "EVO (no WEBDL)",
            IncludeCustomFormatWhenRenaming = false,
            Specifications = new[]
            {
                new CustomFormatSpecificationData
                {
                    Name = "EVO",
                    Implementation = "ReleaseTitleSpecification",
                    Negate = false,
                    Required = true,
                    Fields = new[]
                    {
                        new CustomFormatFieldData
                        {
                            Value = "\\bEVO(TGX)?\\b"
                        }
                    }
                },
                new CustomFormatSpecificationData
                {
                    Name = "WEBDL",
                    Implementation = "SourceSpecification",
                    Negate = true,
                    Required = true,
                    Fields = new[]
                    {
                        new CustomFormatFieldData
                        {
                            Value = 7
                        }
                    }
                },
                new CustomFormatSpecificationData
                {
                    Name = "WEBRIP",
                    Implementation = "SourceSpecification",
                    Negate = true,
                    Required = true,
                    Fields = new[]
                    {
                        new CustomFormatFieldData
                        {
                            Value = 8
                        }
                    }
                }
            }
        });
    }

    [Test, AutoMockData]
    public void Serialize_skips_trash_properties(CustomFormatParser sut)
    {
        var cf = new CustomFormatData
        {
            FileName = "file.json",
            TrashId = "90cedc1fea7ea5d11298bebd3d1d3223",
            TrashScore = -10000,
            Name = "EVO (no WEBDL)",
            IncludeCustomFormatWhenRenaming = false
        };

        var json = JObject.FromObject(cf, ServiceJsonSerializerFactory.Create());

        json.Children<JProperty>().Should().NotContain(x => x.Name.ContainsIgnoreCase("trash"));
    }
}

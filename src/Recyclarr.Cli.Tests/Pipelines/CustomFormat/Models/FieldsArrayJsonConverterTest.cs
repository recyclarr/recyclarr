using Flurl.Http.Configuration;
using Recyclarr.TrashLib.Json;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat.Models;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class FieldsArrayJsonConverterTest
{
    [Test]
    public void Read_multiple_as_array()
    {
        var serializer = new NewtonsoftJsonSerializer(ServiceJsonSerializerFactory.Settings);

        const string json = @"
{
  'fields': [
    {
      'order': 0,
      'name': 'min',
      'label': 'Minimum Size',
      'unit': 'GB',
      'helpText': 'Release must be greater than this size',
      'value': 25,
      'type': 'number',
      'advanced': false
    },
    {
      'order': 1,
      'name': 'max',
      'label': 'Maximum Size',
      'unit': 'GB',
      'helpText': 'Release must be less than or equal to this size',
      'value': 40,
      'type': 'number',
      'advanced': false
    }
  ]
}
";
        var result = serializer.Deserialize<CustomFormatSpecificationData>(json);

        result.Fields.Should().BeEquivalentTo(new[]
        {
            new CustomFormatFieldData
            {
                Value = 25
            },
            new CustomFormatFieldData
            {
                Value = 40
            }
        });
    }

    [Test]
    public void Read_single_as_array()
    {
        var serializer = new NewtonsoftJsonSerializer(ServiceJsonSerializerFactory.Settings);

        const string json = @"
{
  'fields': {
    'order': 0,
    'name': 'min',
    'label': 'Minimum Size',
    'unit': 'GB',
    'helpText': 'Release must be greater than this size',
    'value': 25,
    'type': 'number',
    'advanced': false
  }
}
";
        var result = serializer.Deserialize<CustomFormatSpecificationData>(json);

        result.Fields.Should().BeEquivalentTo(new[]
        {
            new CustomFormatFieldData
            {
                Value = 25
            }
        });
    }

    [Test]
    public void Read_throws_on_unsupported_token_type()
    {
        var serializer = new NewtonsoftJsonSerializer(ServiceJsonSerializerFactory.Settings);

        const string json = @"
{
  'fields': 0
}
";
        var act = () => serializer.Deserialize<CustomFormatSpecificationData>(json);

        act.Should().Throw<InvalidOperationException>();
    }
}

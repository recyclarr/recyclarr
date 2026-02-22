using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.Core.Tests.Yaml;

[TestFixture]
internal sealed class ScalarMismatchDeserializerTest
{
    private IDeserializer _deserializer = null!;

    [SetUp]
    public void Setup()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithNodeDeserializer(new ScalarMismatchDeserializer())
            .Build();
    }

    [Test]
    public void Mapping_where_string_expected_throws_with_clear_message()
    {
        const string yaml = """
            items:
              - key: value
            """;

        var act = () => _deserializer.Deserialize<TestListOfStrings>(yaml);

        // YamlDotNet wraps exceptions thrown by node deserializers; the inner exception
        // carries the user-facing message from ScalarMismatchDeserializer
        act.Should()
            .Throw<YamlException>()
            .Which.InnerException.Should()
            .BeOfType<ConfigParsingException>()
            .Which.Message.Should()
            .Contain("not key-value pairs");
    }

    [Test]
    public void Sequence_where_string_expected_throws_with_clear_message()
    {
        const string yaml = """
            value:
              - nested_list:
                - item
            """;

        var act = () => _deserializer.Deserialize<TestScalarValue>(yaml);

        act.Should()
            .Throw<YamlException>()
            .Which.InnerException.Should()
            .BeOfType<ConfigParsingException>()
            .Which.Message.Should()
            .Contain("not a list");
    }

    [Test]
    public void Scalar_values_deserialize_normally()
    {
        const string yaml = """
            items:
              - plain_string
              - another_string
            """;

        var result = _deserializer.Deserialize<TestListOfStrings>(yaml);

        result.Items.Should().BeEquivalentTo("plain_string", "another_string");
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private sealed class TestListOfStrings
    {
        public List<string> Items { get; set; } = [];
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private sealed class TestScalarValue
    {
        public string? Value { get; set; }
    }
}

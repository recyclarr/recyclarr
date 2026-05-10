using Recyclarr.Common;
using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.Core.Tests.Yaml;

[TestFixture]
internal sealed class DataSizeYamlTypeConverterTest
{
    private IDeserializer _deserializer = null!;

    [SetUp]
    public void Setup()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new DataSizeYamlTypeConverter())
            .Build();
    }

    [Test]
    public void Deserialize_valid_size_string()
    {
        const string yaml = "limit: 256MB";

        var result = _deserializer.Deserialize<TestConfig>(yaml);

        result.Limit.Bytes.Should().Be(256L * 1024 * 1024);
    }

    [Test]
    public void Deserialize_invalid_size_string_throws_yaml_exception()
    {
        const string yaml = "limit: 256";

        var act = () => _deserializer.Deserialize<TestConfig>(yaml);

        act.Should().Throw<YamlException>().WithMessage("*KB, MB, or GB*");
    }

    [Test]
    public void Missing_property_uses_record_default()
    {
        const string yaml = "other: something";

        var result = _deserializer.Deserialize<TestConfigWithDefault>(yaml);

        result.Limit.Should().Be(DataSize.Default);
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private sealed class TestConfig
    {
        public DataSize Limit { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private sealed class TestConfigWithDefault
    {
        public DataSize Limit { get; set; } = DataSize.Default;
        public string? Other { get; set; }
    }
}

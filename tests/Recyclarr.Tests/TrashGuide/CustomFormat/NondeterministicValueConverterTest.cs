using System.Text.Json;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Tests.TrashGuide.CustomFormat;

[Parallelizable(ParallelScope.All)]
public class NondeterministicValueConverterTest
{
    private JsonSerializerOptions _options = default!;

    [SetUp]
    public void Setup()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new NondeterministicValueConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    [Test]
    public void Deserialize_int_value()
    {
        const string json = "42";
        var result = JsonSerializer.Deserialize<object>(json, _options);
        result.Should().Be(42);
    }

    [Test]
    public void Deserialize_double_value()
    {
        const string json = "42.5";
        var result = JsonSerializer.Deserialize<object>(json, _options);
        result.Should().Be(42.5);
    }

    [Test]
    public void Deserialize_string_value()
    {
        const string json = "\"test string\"";
        var result = JsonSerializer.Deserialize<object>(json, _options);
        result.Should().Be("test string");
    }

    [Test]
    public void Deserialize_boolean_value_true()
    {
        const string json = "true";
        var result = JsonSerializer.Deserialize<object>(json, _options);
        result.Should().Be(true);
    }

    [Test]
    public void Deserialize_boolean_value_false()
    {
        const string json = "false";
        var result = JsonSerializer.Deserialize<object>(json, _options);
        result.Should().Be(false);
    }

    [Test]
    public void Deserialize_null_value()
    {
        const string json = "null";
        var result = JsonSerializer.Deserialize<object>(json, _options);
        result.Should().BeNull();
    }

    [Test]
    public void Deserialize_unsupported_type_should_throw()
    {
        const string json = "{ }";
        Action act = () => JsonSerializer.Deserialize<object>(json, _options);
        act.Should()
            .Throw<JsonException>()
            .WithMessage("CF field of type StartObject is not supported*")
            .And.InnerException.Should()
            .BeNull();
    }
}

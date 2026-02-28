using System.Runtime.Serialization;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Serialization;

namespace Recyclarr.Core.Tests.Yaml;

[TestFixture]
internal sealed class DeprecatedPropertyInspectorTest
{
    private static readonly Dictionary<string, RemovedPropertyEntry> TestProperties = new(
        StringComparer.Ordinal
    )
    {
        ["deprecated_prop"] = new RemovedPropertyEntry(
            "The `deprecated_prop` option has been removed.",
            RemovedPropertySeverity.Warning
        ),
        ["errored_prop"] = new RemovedPropertyEntry(
            "The `errored_prop` option has been renamed to `new_prop`.",
            RemovedPropertySeverity.Error
        ),
    };

    [Test]
    public void Known_deprecated_property_records_warning_and_returns_null()
    {
        var inner = Substitute.For<ITypeInspector>();
        var reported = new List<string>();
        var sut = new DeprecatedPropertyInspector(inner, TestProperties, reported.Add);

        inner
            .GetProperty(default!, default, default!, default, default)
            .ReturnsForAnyArgs(_ =>
                throw new SerializationException("Property 'deprecated_prop' not found")
            );

        var result = sut.GetProperty(
            typeof(object),
            null,
            "deprecated_prop",
            ignoreUnmatched: false,
            caseInsensitivePropertyMatching: false
        );

        result.Should().BeNull();
        reported.Should().ContainSingle().Which.Should().Contain("deprecated_prop");
    }

    [Test]
    public void Known_errored_property_throws_config_parsing_exception()
    {
        var inner = Substitute.For<ITypeInspector>();
        var reported = new List<string>();
        var sut = new DeprecatedPropertyInspector(inner, TestProperties, reported.Add);

        inner
            .GetProperty(default!, default, default!, default, default)
            .ReturnsForAnyArgs(_ =>
                throw new SerializationException("Property 'errored_prop' not found")
            );

        var act = () =>
            sut.GetProperty(
                typeof(object),
                null,
                "errored_prop",
                ignoreUnmatched: false,
                caseInsensitivePropertyMatching: false
            );

        act.Should().Throw<ConfigParsingException>().Which.Message.Should().Contain("new_prop");
        reported.Should().BeEmpty();
    }

    [Test]
    public void Known_property_on_type_delegates_normally()
    {
        var inner = Substitute.For<ITypeInspector>();
        var reported = new List<string>();
        var sut = new DeprecatedPropertyInspector(inner, TestProperties, reported.Add);

        var expectedProperty = Substitute.For<IPropertyDescriptor>();
        inner
            .GetProperty(default!, default, default!, default, default)
            .ReturnsForAnyArgs(expectedProperty);

        var result = sut.GetProperty(
            typeof(object),
            null,
            "base_url",
            ignoreUnmatched: false,
            caseInsensitivePropertyMatching: false
        );

        result.Should().BeSameAs(expectedProperty);
        reported.Should().BeEmpty();
    }

    [Test]
    public void Unknown_property_not_in_registry_throws()
    {
        var inner = Substitute.For<ITypeInspector>();
        var reported = new List<string>();
        var sut = new DeprecatedPropertyInspector(inner, TestProperties, reported.Add);

        inner
            .GetProperty(default!, default, default!, default, default)
            .ReturnsForAnyArgs(_ =>
                throw new SerializationException("Property 'totally_unknown' not found")
            );

        var act = () =>
            sut.GetProperty(
                typeof(object),
                null,
                "totally_unknown",
                ignoreUnmatched: false,
                caseInsensitivePropertyMatching: false
            );

        act.Should().Throw<SerializationException>();
        reported.Should().BeEmpty();
    }
}

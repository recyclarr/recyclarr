using System.Runtime.Serialization;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Serialization;

namespace Recyclarr.Core.Tests.Yaml;

[TestFixture]
internal sealed class DeprecatedPropertyInspectorTest
{
    [Test]
    public void Known_deprecated_property_records_warning_and_returns_null()
    {
        var inner = Substitute.For<ITypeInspector>();
        var collector = new ConfigDiagnosticCollector();
        var sut = new DeprecatedPropertyInspector(inner, collector);

        // Inner throws because the property doesn't exist on the type
        inner
            .GetProperty(default!, default, default!, default, default)
            .ReturnsForAnyArgs(_ =>
                throw new SerializationException(
                    "Property 'replace_existing_custom_formats' not found"
                )
            );

        var result = sut.GetProperty(
            typeof(object),
            null,
            "replace_existing_custom_formats",
            ignoreUnmatched: false,
            caseInsensitivePropertyMatching: false
        );

        result.Should().BeNull();
        collector
            .Deprecations.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("replace_existing_custom_formats");
    }

    [Test]
    public void Known_property_on_type_delegates_normally()
    {
        var inner = Substitute.For<ITypeInspector>();
        var collector = new ConfigDiagnosticCollector();
        var sut = new DeprecatedPropertyInspector(inner, collector);

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
        collector.Deprecations.Should().BeEmpty();
    }

    [Test]
    public void Unknown_property_not_in_registry_throws()
    {
        var inner = Substitute.For<ITypeInspector>();
        var collector = new ConfigDiagnosticCollector();
        var sut = new DeprecatedPropertyInspector(inner, collector);

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
        collector.Deprecations.Should().BeEmpty();
    }
}

using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat.Models;

internal sealed class CustomFormatResourceComparerTest
{
    [Test]
    public void Equivalent_when_all_properties_match()
    {
        var a = CreateMockCustomFormatResource();
        var b = CreateMockCustomFormatResource();

        a.IsEquivalentTo(b).Should().BeTrue();
    }

    [Test]
    public void Equivalent_when_ignored_fields_are_different()
    {
        var a = new CustomFormatResource
        {
            Id = 1,
            Name = "Test",
            TrashId = "a",
            TrashScores = { ["default"] = 1 },
            Category = "one",
        };

        var b = new CustomFormatResource
        {
            Id = 1,
            Name = "Test",
            TrashId = "b",
            TrashScores = { ["default"] = 2 },
            Category = "two",
        };

        a.IsEquivalentTo(b).Should().BeTrue();
    }

    [Test]
    public void Not_equivalent_when_other_is_null()
    {
        var a = new CustomFormatResource();

        a.IsEquivalentTo(null).Should().BeFalse();
    }

    [Test]
    public void Equivalent_for_same_reference()
    {
        var a = new CustomFormatResource();

        a.IsEquivalentTo(a).Should().BeTrue();
    }

    [Test]
    public void Not_equivalent_when_different_spec_count()
    {
        var a = CreateMockCustomFormatResource();
        var b = a with
        {
            Specifications = a
                .Specifications.Concat([new CustomFormatSpecificationData { Name = "Extra" }])
                .ToList(),
        };

        a.IsEquivalentTo(b).Should().BeFalse();
    }

    [Test]
    public void Not_equivalent_when_spec_names_differ()
    {
        var a = CreateMockCustomFormatResource();
        var b = a with
        {
            Specifications = a
                .Specifications.Select(spec =>
                    spec with
                    {
                        Name = spec.Name == "WEBRIP" ? "DIFFERENT_NAME" : spec.Name,
                    }
                )
                .ToList(),
        };

        a.IsEquivalentTo(b).Should().BeFalse();
    }

    [Test]
    public void Equivalent_when_extra_fields_in_service_response()
    {
        var a = CreateMockCustomFormatResource();
        var b = a with
        {
            Specifications = a
                .Specifications.Select(spec =>
                    spec with
                    {
                        Fields = spec
                            .Fields.Concat([
                                new CustomFormatFieldData
                                {
                                    Name = "AdditionalField",
                                    Value = "ExtraValue",
                                },
                            ])
                            .ToList(),
                    }
                )
                .ToList(),
        };

        a.IsEquivalentTo(b).Should().BeTrue();
    }

    [Test]
    public void Equivalent_when_spec_and_field_order_differs()
    {
        var a = CreateMockCustomFormatResource();
        var b = a with
        {
            Specifications = a
                .Specifications.Reverse()
                .Select(spec => spec with { Fields = spec.Fields.Reverse().ToList() })
                .ToList(),
        };

        a.IsEquivalentTo(b).Should().BeTrue();
    }

    [Test]
    public void Equivalent_across_derived_and_base_types()
    {
        // This is the critical test: guide CFs are SonarrCustomFormatResource,
        // but API responses deserialize to base CustomFormatResource.
        // IsEquivalentTo() must work across this type boundary.
        var guideCf = new SonarrCustomFormatResource
        {
            Id = 1,
            Name = "Test CF",
            TrashId = "abc123",
            IncludeCustomFormatWhenRenaming = false,
            Specifications =
            [
                new CustomFormatSpecificationData
                {
                    Name = "TestSpec",
                    Implementation = "ReleaseTitleSpecification",
                    Negate = false,
                    Required = true,
                    Fields = [new CustomFormatFieldData { Name = "value", Value = "test" }],
                },
            ],
        };

        var serviceCf = new CustomFormatResource
        {
            Id = 1,
            Name = "Test CF",
            IncludeCustomFormatWhenRenaming = false,
            Specifications =
            [
                new CustomFormatSpecificationData
                {
                    Name = "TestSpec",
                    Implementation = "ReleaseTitleSpecification",
                    Negate = false,
                    Required = true,
                    Fields = [new CustomFormatFieldData { Name = "value", Value = "test" }],
                },
            ],
        };

        guideCf.IsEquivalentTo(serviceCf).Should().BeTrue();
    }

    [TestCase(typeof(CustomFormatSpecificationData))]
    public void Throws_exception_when_used_as_key_in_dictionary(Type type)
    {
        var act = () =>
            new Dictionary<object, object?>().Add(Activator.CreateInstance(type)!, null);

        act.Should().Throw<NotImplementedException>();
    }

    [TestCase(typeof(CustomFormatSpecificationData))]
    public void Throws_exception_when_used_as_key_in_hash_set(Type type)
    {
        var act = () => new HashSet<object>().Add(Activator.CreateInstance(type)!);

        act.Should().Throw<NotImplementedException>();
    }

    private static CustomFormatResource CreateMockCustomFormatResource()
    {
        return new CustomFormatResource
        {
            Id = 1,
            Name = "EVO (no WEBDL)",
            IncludeCustomFormatWhenRenaming = false,
            Specifications =
            [
                new CustomFormatSpecificationData
                {
                    Name = "EVO",
                    Implementation = "ReleaseTitleSpecification",
                    Negate = false,
                    Required = true,
                    Fields =
                    [
                        new CustomFormatFieldData { Name = "value", Value = @"\bEVO(TGX)?\b" },
                        new CustomFormatFieldData { Name = "foo1", Value = "foo1" },
                    ],
                },
                new CustomFormatSpecificationData
                {
                    Name = "WEBDL",
                    Implementation = "SourceSpecification",
                    Negate = true,
                    Required = true,
                    Fields =
                    [
                        new CustomFormatFieldData { Name = "value", Value = 7 },
                        new CustomFormatFieldData { Name = "foo2", Value = "foo2" },
                    ],
                },
                new CustomFormatSpecificationData
                {
                    Name = "WEBRIP",
                    Implementation = "LanguageSpecification",
                    Negate = true,
                    Required = true,
                    Fields =
                    [
                        new CustomFormatFieldData { Name = "value", Value = 8 },
                        new CustomFormatFieldData { Name = "exceptLanguage", Value = false },
                        new CustomFormatFieldData { Name = "foo3", Value = "foo3" },
                    ],
                },
            ],
        };
    }
}

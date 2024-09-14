using System.Diagnostics.CodeAnalysis;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat.Models;

[Parallelizable(ParallelScope.All)]
public class CustomFormatDataComparerTest
{
    [Test]
    public void Custom_formats_equal()
    {
        var a = CreateMockCustomFormatData();

        var b = CreateMockCustomFormatData();

        a.Should().BeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Custom_formats_not_equal_when_field_value_different()
    {
        var a = CreateMockCustomFormatData();

        var b = CreateMockCustomFormatData() with
        {
            Specifications = a.Specifications.Select(spec => spec with
            {
                Name = spec.Name == "WEBRIP" ? "WEBRIP2" : spec.Name
            }).ToList()
        };

        a.Should().NotBeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Equal_when_ignored_fields_are_different()
    {
        var a = new CustomFormatData
        {
            TrashId = "a",
            TrashScores = {["default"] = 1},
            Category = "one"
        };

        var b = new CustomFormatData
        {
            TrashId = "b",
            TrashScores = {["default"] = 2},
            Category = "two"
        };

        a.Should().BeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Not_equal_when_right_is_null()
    {
        var a = new CustomFormatData();
        CustomFormatData? b = null;

        a.Should().NotBeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Not_equal_when_left_is_null()
    {
        CustomFormatData? a = null;
        var b = new CustomFormatData();

        a.Should().NotBeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Equal_for_same_reference()
    {
        var a = new CustomFormatData();

        a.Should().BeEquivalentTo(a, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Not_equal_when_different_spec_count()
    {
        var a = CreateMockCustomFormatData();

        var b = a with
        {
            Specifications = a.Specifications.Concat([new CustomFormatSpecificationData()]).ToList()
        };

        a.Should().NotBeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Not_equal_when_non_matching_spec_names()
    {
        var a = CreateMockCustomFormatData();

        var b = a with
        {
            Specifications = a.Specifications.Select(spec => spec with
            {
                Name = spec.Name == "WEBRIP" ? "WEBRIP2" : spec.Name
            }).ToList()
        };

        a.Should().NotBeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Not_equal_when_different_spec_names_and_values()
    {
        var a = CreateMockCustomFormatData();
        var b = a with
        {
            Specifications = a.Specifications.Select(spec => spec with
            {
                Name = spec.Name == "WEBRIP" ? "UNIQUE_NAME" : spec.Name,
                Fields = spec.Fields.Select(field => field with
                {
                    Value = field.Value is int ? 99 : "NEW_VALUE"
                }).ToList()
            }).ToList()
        };

        a.Should().NotBeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Equal_when_different_field_counts_but_same_names_and_values()
    {
        var a = CreateMockCustomFormatData();
        var b = a with
        {
            Specifications = a.Specifications.Select(spec => spec with
            {
                Fields = spec.Fields
                    .Concat([new CustomFormatFieldData {Name = "AdditionalField", Value = "ExtraValue"}])
                    .ToList()
            }).ToList()
        };

        a.Should().BeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Equal_when_specifications_order_different()
    {
        var a = CreateMockCustomFormatData();

        var b = a with
        {
            Specifications = a.Specifications.Reverse().ToList()
        };

        a.Should().BeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [Test]
    public void Equal_when_fields_order_different_for_each_specification()
    {
        var a = CreateMockCustomFormatData();

        var b = a with
        {
            Specifications = a.Specifications.Select(spec => spec with
            {
                Fields = spec.Fields.Reverse().ToList()
            }).ToList()
        };

        a.Should().BeEquivalentTo(b, o => o.ComparingRecordsByValue());
    }

    [TestCase(typeof(CustomFormatData))]
    [TestCase(typeof(CustomFormatSpecificationData))]
    public void Throws_exception_when_used_as_key_in_dictionary(Type type)
    {
        var act = () => new Dictionary<object, object?>().Add(Activator.CreateInstance(type)!, null);

        act.Should().Throw<NotImplementedException>();
    }

    [TestCase(typeof(CustomFormatData))]
    [TestCase(typeof(CustomFormatSpecificationData))]
    public void Throws_exception_when_used_as_key_in_hash_set(Type type)
    {
        var act = () => new HashSet<object>().Add(Activator.CreateInstance(type)!);

        act.Should().Throw<NotImplementedException>();
    }

    private static CustomFormatData CreateMockCustomFormatData()
    {
        return new CustomFormatData
        {
            Name = "EVO (no WEBDL)",
            IncludeCustomFormatWhenRenaming = false,
            Specifications = new List<CustomFormatSpecificationData>
            {
                new()
                {
                    Name = "EVO",
                    Implementation = "ReleaseTitleSpecification",
                    Negate = false,
                    Required = true,
                    Fields = new List<CustomFormatFieldData>
                    {
                        new() {Name = "value", Value = @"\bEVO(TGX)?\b"},
                        new() {Name = "foo1", Value = "foo1"}
                    }
                },
                new()
                {
                    Name = "WEBDL",
                    Implementation = "SourceSpecification",
                    Negate = true,
                    Required = true,
                    Fields = new List<CustomFormatFieldData>
                    {
                        new() {Name = "value", Value = 7},
                        new() {Name = "foo2", Value = "foo2"}
                    }
                },
                new()
                {
                    Name = "WEBRIP",
                    Implementation = "LanguageSpecification",
                    Negate = true,
                    Required = true,
                    Fields = new List<CustomFormatFieldData>
                    {
                        new() {Name = "value", Value = 8},
                        new() {Name = "exceptLanguage", Value = false},
                        new() {Name = "foo3", Value = "foo3"}
                    }
                }
            }
        };
    }
}

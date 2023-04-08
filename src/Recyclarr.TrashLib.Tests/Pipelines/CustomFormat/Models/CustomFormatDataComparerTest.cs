using Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

namespace Recyclarr.TrashLib.Tests.Pipelines.CustomFormat.Models;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatDataComparerTest
{
    [Test]
    public void Custom_formats_equal()
    {
        var a = new CustomFormatData
        {
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
        };

        var b = new CustomFormatData
        {
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
        };

        a.Should().BeEquivalentTo(b, o => o.Using(CustomFormatData.Comparer));
    }

    [Test]
    public void Custom_formats_not_equal_when_field_value_different()
    {
        var a = new CustomFormatData
        {
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
        };

        var b = new CustomFormatData
        {
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
                            Value = 10 // this is different
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
        };

        var result = CustomFormatData.Comparer.Equals(a, b);

        result.Should().BeFalse();
    }

    [Test]
    public void Equal_when_ignored_fields_are_different()
    {
        var a = new CustomFormatData
        {
            FileName = "file1.json",
            TrashId = "a",
            TrashScore = 1,
            Category = "one"
        };

        var b = new CustomFormatData
        {
            FileName = "file2.json",
            TrashId = "b",
            TrashScore = 2,
            Category = "two"
        };

        var result = CustomFormatData.Comparer.Equals(a, b);

        result.Should().BeTrue();
    }

    [Test]
    public void Not_equal_when_right_is_null()
    {
        var a = new CustomFormatData();
        var b = (CustomFormatData?) null;

        var result = CustomFormatData.Comparer.Equals(a, b);

        result.Should().BeFalse();
    }

    [Test]
    public void Not_equal_when_left_is_null()
    {
        var a = (CustomFormatData?) null;
        var b = new CustomFormatData();

        var result = CustomFormatData.Comparer.Equals(a, b);

        result.Should().BeFalse();
    }

    [Test]
    public void Equal_for_same_reference()
    {
        var a = new CustomFormatData();

        var result = CustomFormatData.Comparer.Equals(a, a);

        result.Should().BeTrue();
    }

    [Test]
    public void Not_equal_when_different_spec_count()
    {
        var a = new CustomFormatData
        {
            Name = "EVO (no WEBDL)",
            IncludeCustomFormatWhenRenaming = false,
            Specifications = new[]
            {
                new CustomFormatSpecificationData(),
                new CustomFormatSpecificationData()
            }
        };

        var b = new CustomFormatData
        {
            Name = "EVO (no WEBDL)",
            IncludeCustomFormatWhenRenaming = false,
            Specifications = new[]
            {
                new CustomFormatSpecificationData(),
                new CustomFormatSpecificationData(),
                new CustomFormatSpecificationData()
            }
        };

        var result = CustomFormatData.Comparer.Equals(a, b);

        result.Should().BeFalse();
    }

    [Test]
    public void Not_equal_when_non_matching_spec_names()
    {
        var a = new CustomFormatData
        {
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
        };

        var b = new CustomFormatData
        {
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
                    Name = "WEBRIP2", // This name is different
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
        };

        var result = CustomFormatData.Comparer.Equals(a, b);

        result.Should().BeFalse();
    }
}

using System.Collections.Generic;
using System.Linq;
using Common;
using FluentAssertions;
using NUnit.Framework;
using Serilog;
using Serilog.Sinks.TestCorrelator;
using TestLibrary;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.ReleaseProfile;

namespace TrashLib.Tests.Sonarr.ReleaseProfile
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class ReleaseProfileParserTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // Formatter.AddFormatter(new ProfileDataValueFormatter());
        }

        private class Context
        {
            public Context()
            {
                var logger = new LoggerConfiguration()
                    .WriteTo.TestCorrelator()
                    .MinimumLevel.Debug()
                    .CreateLogger();

                Config = new SonarrConfiguration
                {
                    ReleaseProfiles = new[] {new ReleaseProfileConfig()}
                };

                GuideParser = new ReleaseProfileGuideParser(logger);
            }

            public SonarrConfiguration Config { get; }
            public ReleaseProfileGuideParser GuideParser { get; }
            public ResourceDataReader TestData { get; } = new(typeof(ReleaseProfileParserTest), "Data");

            public IDictionary<string, ProfileData> ParseWithDefaults(string markdown)
            {
                return GuideParser.ParseMarkdown(Config.ReleaseProfiles.First(), markdown);
            }
        }

        [Test]
        public void Parse_CodeBlockScopedCategories_CategoriesSwitch()
        {
            var markdown = StringUtils.TrimmedString(@"
# Test Release Profile

Add this to must not contain (ignored)

```
abc
```

Add this to must contain (required)

```
xyz
```
");
            var context = new Context();
            var results = context.ParseWithDefaults(markdown);

            results.Should().ContainKey("Test Release Profile")
                .WhichValue.Should().BeEquivalentTo(new
                {
                    Ignored = new List<string> {"abc"},
                    Required = new List<string> {"xyz"}
                });
        }

        [Test]
        public void Parse_HeaderCategoryFollowedByCodeBlockCategories_CodeBlockChangesCurrentCategory()
        {
            var markdown = StringUtils.TrimmedString(@"
# Test Release Profile

## Must Not Contain

Add this one

```
abc
```

Add this to must contain (required)

```
xyz
```

One more

```
123
```
");
            var context = new Context();
            var results = context.ParseWithDefaults(markdown);

            results.Should().ContainKey("Test Release Profile")
                .WhichValue.Should().BeEquivalentTo(new
                {
                    Ignored = new List<string> {"abc"},
                    Required = new List<string> {"xyz", "123"}
                });
        }

        [Test]
        public void Parse_IgnoredRequiredPreferredScores()
        {
            var context = new Context();
            var markdown = context.TestData.ReadData("test_parse_markdown_complete_doc.md");
            var results = context.GuideParser.ParseMarkdown(context.Config.ReleaseProfiles.First(), markdown);

            results.Count.Should().Be(1);

            var profile = results.First().Value;

            profile.Ignored.Should().BeEquivalentTo("term2", "term3");
            profile.Required.Should().BeEquivalentTo("term4");
            profile.Preferred.Should().ContainKey(100).WhichValue.Should().BeEquivalentTo(new List<string> {"term1"});
        }

        [Test]
        public void Parse_IncludePreferredWhenRenaming()
        {
            var context = new Context();
            var markdown = context.TestData.ReadData("include_preferred_when_renaming.md");
            var results = context.ParseWithDefaults(markdown);

            results.Should()
                .ContainKey("First Release Profile")
                .WhichValue.IncludePreferredWhenRenaming.Should().Be(true);
            results.Should()
                .ContainKey("Second Release Profile")
                .WhichValue.IncludePreferredWhenRenaming.Should().Be(false);
        }

        [Test]
        public void Parse_IndentedIncludePreferred_ShouldBeParsed()
        {
            var markdown = StringUtils.TrimmedString(@"
# Release Profile 1

!!! Warning
    Do not check include preferred

must contain

```
test1
```

# Release Profile 2

!!! Warning
    Check include preferred

must contain

```
test2
```
");
            var context = new Context();
            var results = context.ParseWithDefaults(markdown);

            var expectedResults = new Dictionary<string, ProfileData>
            {
                {
                    "Release Profile 1", new ProfileData
                    {
                        IncludePreferredWhenRenaming = false,
                        Required = new List<string> {"test1"}
                    }
                },
                {
                    "Release Profile 2", new ProfileData
                    {
                        IncludePreferredWhenRenaming = true,
                        Required = new List<string> {"test2"}
                    }
                }
            };

            results.Should().BeEquivalentTo(expectedResults);
        }

        [Test]
        public void Parse_OptionalTerms_AreCapturedProperly()
        {
            var markdown = StringUtils.TrimmedString(@"
# Optional Release Profile

```
skipped1
```

## Must Not Contain

```
optional1
```

## Preferred

score [10]

```
optional2
```

One more must contain:

```
optional3
```

# Second Release Profile

This must not contain:

```
not-optional1
```
");
            var context = new Context();
            var results = context.ParseWithDefaults(markdown);

            var expectedResults = new Dictionary<string, ProfileData>
            {
                {
                    "Optional Release Profile", new ProfileData
                    {
                        Optional = new ProfileDataOptional
                        {
                            Ignored = new List<string> {"optional1"},
                            Required = new List<string> {"optional3"},
                            Preferred = new Dictionary<int, List<string>>
                            {
                                {10, new List<string> {"optional2"}}
                            }
                        }
                    }
                },
                {
                    "Second Release Profile", new ProfileData
                    {
                        Ignored = new List<string> {"not-optional1"}
                    }
                }
            };

            results.Should().BeEquivalentTo(expectedResults);
        }

        [Test]
        public void Parse_PotentialScore_WarningLogged()
        {
            string markdown = StringUtils.TrimmedString(@"
# First Release Profile

The below line should be a score but isn't because it's missing the word 'score'.

Use this number [100]

```
abc
```
");
            var context = new Context();
            var results = context.ParseWithDefaults(markdown);

            results.Should().BeEmpty();

            const string expectedLog =
                "Found a potential score on line #5 that will be ignored because the " +
                "word 'score' is missing (This is probably a bug in the guide itself): \"[100]\"";

            TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().ContainSingle(evt => evt.RenderMessage(default) == expectedLog);
        }

        [Test]
        public void Parse_ScoreWithoutCategory_ImplicitlyPreferred()
        {
            var markdown = StringUtils.TrimmedString(@"
# Test Release Profile

score is [100]

```
abc
```
");
            var context = new Context();
            var results = context.ParseWithDefaults(markdown);

            results.Should()
                .ContainKey("Test Release Profile")
                .WhichValue.Preferred.Should()
                .BeEquivalentTo(new Dictionary<int, List<string>>
                {
                    {100, new List<string> {"abc"}}
                });
        }

        [Test]
        public void Parse_SkippableLines_AreSkippedWithLog()
        {
            var markdown = StringUtils.TrimmedString(@"
# First Release Profile

!!! Admonition lines are skipped
    Indented lines are skipped
");
            // List of substrings of logs that should appear in the resulting list of logs after parsing is done.
            // We are only looking for logs relevant to the skipped lines we're testing for.
            var expectedLogs = new List<string>
            {
                "Skip Admonition",
                "Skip Indented Line"
            };

            var context = new Context();
            var results = context.ParseWithDefaults(markdown);

            results.Should().BeEmpty();

            var ctx = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
            foreach (var log in expectedLogs)
            {
                ctx.Should().Contain(evt => evt.MessageTemplate.Text.Contains(log));
            }
        }

        [Test]
        public void Parse_StrictNegativeScores()
        {
            var context = new Context();
            context.Config.ReleaseProfiles = new List<ReleaseProfileConfig>
            {
                new() {StrictNegativeScores = true}
            };

            var markdown = context.TestData.ReadData("strict_negative_scores.md");
            var results = context.ParseWithDefaults(markdown);

            results.Should()
                .ContainKey("Test Release Profile")
                .WhichValue.Should()
                .BeEquivalentTo(new
                {
                    Required = new { },
                    Ignored = new List<string> {"abc"},
                    Preferred = new Dictionary<int, List<string>> {{0, new List<string> {"xyz"}}}
                });
        }

        [Test]
        public void Parse_TermsWithoutCategory_AreSkipped()
        {
            var markdown = StringUtils.TrimmedString(@"
# Test Release Profile

```
skipped1
```

## Must Not Contain

```
added1
```

## Preferred

score [10]

```
added2
```

One more

```
added3
```

# Second Release Profile

```
skipped2
```
");
            var context = new Context();
            var results = context.ParseWithDefaults(markdown);

            var expectedResults = new Dictionary<string, ProfileData>
            {
                {
                    "Test Release Profile", new ProfileData
                    {
                        Ignored = new List<string> {"added1"},
                        Preferred = new Dictionary<int, List<string>>
                        {
                            {10, new List<string> {"added2", "added3"}}
                        }
                    }
                }
            };

            results.Should().BeEquivalentTo(expectedResults);
        }
    }
}

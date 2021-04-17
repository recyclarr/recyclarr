using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using Serilog.Sinks.TestCorrelator;
using TestLibrary;
using Trash.Sonarr;
using Trash.Sonarr.ReleaseProfile;

namespace Trash.Tests.Sonarr.ReleaseProfile
{
    [TestFixture]
    public class ReleaseProfileParserTest
    {
        private class Context
        {
            public Context()
            {
                var logger = new LoggerConfiguration()
                    .WriteTo.TestCorrelator()
                    .MinimumLevel.Debug()
                    .CreateLogger();

                Config = Substitute.For<SonarrConfiguration>();
                Config.ReleaseProfiles.Add(new ReleaseProfileConfig());

                GuideParser = new ReleaseProfileGuideParser(logger);
            }

            public SonarrConfiguration Config { get; }
            public ReleaseProfileGuideParser GuideParser { get; }
            public TestData<ReleaseProfileParserTest> TestData { get; } = new();
        }

        [Test]
        public void Parse_IgnoredRequiredPreferredScores()
        {
            var context = new Context();
            var markdown = context.TestData.GetResourceData("test_parse_markdown_complete_doc.md");
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
            var markdown = context.TestData.GetResourceData("include_preferred_when_renaming.md");
            var results = context.GuideParser.ParseMarkdown(context.Config.ReleaseProfiles.First(), markdown);

            results.Should()
                .ContainKey("First Release Profile")
                .WhichValue.IncludePreferredWhenRenaming.Should().Be(true);
            results.Should()
                .ContainKey("Second Release Profile")
                .WhichValue.IncludePreferredWhenRenaming.Should().Be(false);
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
            var results = context.GuideParser.ParseMarkdown(context.Config.ReleaseProfiles.First(), markdown);

            results.Should().ContainKey("First Release Profile")
                .WhichValue.Should().BeEquivalentTo(new ProfileData());

            const string expectedLog =
                "Found a potential score on line #5 that will be ignored because the " +
                "word 'score' is missing (This is probably a bug in the guide itself): \"[100]\"";

            TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().ContainSingle(evt => evt.RenderMessage(default) == expectedLog);
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
            var results = context.GuideParser.ParseMarkdown(context.Config.ReleaseProfiles.First(), markdown);

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

            var markdown = context.TestData.GetResourceData("strict_negative_scores.md");
            var results = context.GuideParser.ParseMarkdown(context.Config.ReleaseProfiles.First(), markdown);

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
    }
}

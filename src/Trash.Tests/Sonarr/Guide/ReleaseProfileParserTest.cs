using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestLibrary;
using Trash.Sonarr;
using Trash.Sonarr.ReleaseProfile;

namespace Trash.Tests.Sonarr.Guide
{
    [TestFixture]
    public class ReleaseProfileParserTest
    {
        private class Context
        {
            public Context()
            {
                Config = Substitute.For<SonarrConfiguration>();
                GuideParser = new ReleaseProfileGuideParser();
            }

            public SonarrConfiguration Config { get; }
            public ReleaseProfileGuideParser GuideParser { get; }
            public TestData<ReleaseProfileParserTest> TestData { get; } = new();
        }

        [Test]
        public void Parse_IgnoredRequiredPreferredScores()
        {
            var context = new Context();
            context.Config.ReleaseProfiles.Add(new ReleaseProfileConfig());

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
            context.Config.ReleaseProfiles.Add(new ReleaseProfileConfig());

            var markdown = context.TestData.GetResourceData("include_preferred_when_renaming.md");
            var results = context.GuideParser.ParseMarkdown(context.Config.ReleaseProfiles.First(), markdown);

            results.Should()
                .ContainKey("First Release Profile")
                .WhichValue.IncludePreferredWhenRenaming.Should()
                .Be(true);
            results.Should()
                .ContainKey("Second Release Profile")
                .WhichValue.IncludePreferredWhenRenaming.Should()
                .Be(false);
        }

        [Test]
        public void Parse_StrictNegativeScores()
        {
            var context = new Context();
            context.Config.ReleaseProfiles.Add(new ReleaseProfileConfig
            {
                // Pretend the user specified this option for testing purposes
                StrictNegativeScores = true
            });

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

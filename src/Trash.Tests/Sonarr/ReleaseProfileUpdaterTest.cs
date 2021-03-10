using NSubstitute;
using NUnit.Framework;
using Trash.Sonarr;
using Trash.Sonarr.Api;
using Trash.Sonarr.ReleaseProfile;

namespace Trash.Tests.Sonarr
{
    [TestFixture]
    public class ReleaseProfileUpdaterTest
    {
        [Test]
        public void ProcessReleaseProfile_InvalidReleaseProfiles_NoCrashNoCalls()
        {
            var args = Substitute.For<ISonarrCommand>();
            var parser = Substitute.For<IReleaseProfileGuideParser>();
            var api = Substitute.For<ISonarrApi>();
            var config = Substitute.For<SonarrConfiguration>();

            var logic = new ReleaseProfileUpdater(parser, api);
            logic.Process(args, config);

            parser.DidNotReceive().GetMarkdownData(Arg.Any<ReleaseProfileType>());
        }

        [Test]
        public void ProcessReleaseProfile_SingleProfilePreview()
        {
            var parser = Substitute.For<IReleaseProfileGuideParser>();
            var api = Substitute.For<ISonarrApi>();
            var config = Substitute.For<SonarrConfiguration>();
            var args = Substitute.For<ISonarrCommand>();

            parser.GetMarkdownData(ReleaseProfileType.Anime).Returns("theMarkdown");
            config.ReleaseProfiles.Add(new ReleaseProfileConfig {Type = ReleaseProfileType.Anime});

            var updater = new ReleaseProfileUpdater(parser, api);
            updater.Process(args, config);

            parser.Received().ParseMarkdown(config.ReleaseProfiles[0], "theMarkdown");
        }
    }
}

using NSubstitute;
using NUnit.Framework;
using Serilog;
using Trash.Sonarr;
using Trash.Sonarr.Api;
using Trash.Sonarr.ReleaseProfile;

namespace Trash.Tests.Sonarr
{
    [TestFixture]
    public class ReleaseProfileUpdaterTest
    {
        private class Context
        {
            public ISonarrCommand Args { get; } = Substitute.For<ISonarrCommand>();
            public IReleaseProfileGuideParser Parser { get; } = Substitute.For<IReleaseProfileGuideParser>();
            public ISonarrApi Api { get; } = Substitute.For<ISonarrApi>();
            public SonarrConfiguration Config { get; } = Substitute.For<SonarrConfiguration>();
            public ILogger Logger { get; } = Substitute.For<ILogger>();
        }

        [Test]
        public void ProcessReleaseProfile_InvalidReleaseProfiles_NoCrashNoCalls()
        {
            var context = new Context();

            var logic = new ReleaseProfileUpdater(context.Logger, context.Parser, context.Api);
            logic.Process(context.Args, context.Config);

            context.Parser.DidNotReceive().GetMarkdownData(Arg.Any<ReleaseProfileType>());
        }

        [Test]
        public void ProcessReleaseProfile_SingleProfilePreview()
        {
            var context = new Context();

            context.Parser.GetMarkdownData(ReleaseProfileType.Anime).Returns("theMarkdown");
            context.Config.ReleaseProfiles.Add(new ReleaseProfileConfig {Type = ReleaseProfileType.Anime});

            var logic = new ReleaseProfileUpdater(context.Logger, context.Parser, context.Api);
            logic.Process(context.Args, context.Config);

            context.Parser.Received().ParseMarkdown(context.Config.ReleaseProfiles[0], "theMarkdown");
        }
    }
}

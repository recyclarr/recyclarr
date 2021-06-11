using NSubstitute;
using NUnit.Framework;
using Serilog;
using TrashLib.Sonarr;
using TrashLib.Sonarr.Api;
using TrashLib.Sonarr.ReleaseProfile;

namespace TrashLib.Tests.Sonarr
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class ReleaseProfileUpdaterTest
    {
        private class Context
        {
            public IReleaseProfileGuideParser Parser { get; } = Substitute.For<IReleaseProfileGuideParser>();
            public ISonarrApi Api { get; } = Substitute.For<ISonarrApi>();
            public ILogger Logger { get; } = Substitute.For<ILogger>();
        }

        [Test]
        public void ProcessReleaseProfile_InvalidReleaseProfiles_NoCrashNoCalls()
        {
            var context = new Context();

            var logic = new ReleaseProfileUpdater(context.Logger, context.Parser, context.Api);
            logic.Process(false, new SonarrConfiguration());

            context.Parser.DidNotReceive().GetMarkdownData(Arg.Any<ReleaseProfileType>());
        }

        [Test]
        public void ProcessReleaseProfile_SingleProfilePreview()
        {
            var context = new Context();

            context.Parser.GetMarkdownData(ReleaseProfileType.Anime).Returns("theMarkdown");
            var config = new SonarrConfiguration
            {
                ReleaseProfiles = new[] {new ReleaseProfileConfig {Type = ReleaseProfileType.Anime}}
            };

            var logic = new ReleaseProfileUpdater(context.Logger, context.Parser, context.Api);
            logic.Process(false, config);

            context.Parser.Received().ParseMarkdown(config.ReleaseProfiles[0], "theMarkdown");
        }
    }
}

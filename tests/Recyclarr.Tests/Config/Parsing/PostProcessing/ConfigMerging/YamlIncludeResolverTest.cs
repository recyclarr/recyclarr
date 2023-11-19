using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class YamlIncludeResolverTest
{
    [Test]
    public void Find_and_return_processor()
    {
        var processors = new[]
        {
            Substitute.For<IIncludeProcessor>(),
            Substitute.For<IIncludeProcessor>()
        };

        processors[1].CanProcess(default!).ReturnsForAnyArgs(true);
        processors[1].GetPathToConfig(default!, default!).ReturnsForAnyArgs(_ =>
        {
            var fileInfo = Substitute.For<IFileInfo>();
            fileInfo.Exists.Returns(true);
            fileInfo.FullName.Returns("the_path");
            return fileInfo;
        });

        var sut = new YamlIncludeResolver(processors);
        var result = sut.GetIncludePath(Substitute.For<IYamlInclude>(), SupportedServices.Radarr);

        result.FullName.Should().Be("the_path");
    }

    [Test]
    public void Throw_when_no_matching_processor()
    {
        var processors = new[]
        {
            Substitute.For<IIncludeProcessor>(),
            Substitute.For<IIncludeProcessor>()
        };

        var sut = new YamlIncludeResolver(processors);
        var act = () => sut.GetIncludePath(Substitute.For<IYamlInclude>(), SupportedServices.Radarr);

        act.Should().Throw<YamlIncludeException>().WithMessage("*type is not supported*");
    }

    [Test]
    public void Throw_when_path_does_not_exist()
    {
        var processors = new[]
        {
            Substitute.For<IIncludeProcessor>(),
            Substitute.For<IIncludeProcessor>()
        };

        processors[1].CanProcess(default!).ReturnsForAnyArgs(true);
        processors[1].GetPathToConfig(default!, default!).ReturnsForAnyArgs(_ =>
        {
            var fileInfo = Substitute.For<IFileInfo>();
            fileInfo.Exists.Returns(false);
            return fileInfo;
        });

        var sut = new YamlIncludeResolver(processors);
        var act = () => sut.GetIncludePath(Substitute.For<IYamlInclude>(), SupportedServices.Radarr);

        act.Should().Throw<YamlIncludeException>().WithMessage("*does not exist*");
    }
}

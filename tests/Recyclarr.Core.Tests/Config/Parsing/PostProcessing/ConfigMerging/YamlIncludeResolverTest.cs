using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class YamlIncludeResolverTest
{
    public abstract class TestYamlInclude1 : IYamlInclude;

    public abstract class TestYamlInclude2 : IYamlInclude;

    public abstract class TestYamlInclude3 : IYamlInclude;

    [Test, AutoMockData]
    public void Find_and_return_processor(
        [Frozen(Matching.ImplementedInterfaces)] StubAutofacIndex<Type, IIncludeProcessor> index,
        YamlIncludeResolver sut
    )
    {
        var processors = new List<(IYamlInclude Directive, IIncludeProcessor Value)>
        {
            (Substitute.ForPartsOf<TestYamlInclude1>(), Substitute.For<IIncludeProcessor>()),
            (Substitute.ForPartsOf<TestYamlInclude2>(), Substitute.For<IIncludeProcessor>()),
        };

        index.AddRange(processors.Select(x => (x.Directive.GetType(), x.Value)));
        processors[1]
            .Value.GetPathToConfig(default!, default!)
            .ReturnsForAnyArgs(_ =>
            {
                var fileInfo = Substitute.For<IFileInfo>();
                fileInfo.Exists.Returns(true);
                fileInfo.FullName.Returns("the_path");
                return fileInfo;
            });

        var result = sut.GetIncludePath(processors[1].Directive, SupportedServices.Radarr);

        result.FullName.Should().Be("the_path");
    }

    [Test, AutoMockData]
    public void Throw_when_no_matching_processor(
        [Frozen(Matching.ImplementedInterfaces)] StubAutofacIndex<Type, IIncludeProcessor> index,
        YamlIncludeResolver sut
    )
    {
        var processors = new List<(IYamlInclude Directive, IIncludeProcessor Value)>
        {
            (Substitute.ForPartsOf<TestYamlInclude1>(), Substitute.For<IIncludeProcessor>()),
            (Substitute.ForPartsOf<TestYamlInclude2>(), Substitute.For<IIncludeProcessor>()),
        };

        index.AddRange(processors.Select(x => (x.Directive.GetType(), x.Value)));

        var act = () =>
            sut.GetIncludePath(Substitute.ForPartsOf<TestYamlInclude3>(), SupportedServices.Radarr);

        act.Should().Throw<YamlIncludeException>().WithMessage("*type is not supported*");
    }

    [Test, AutoMockData]
    public void Throw_when_path_does_not_exist(
        [Frozen(Matching.ImplementedInterfaces)] StubAutofacIndex<Type, IIncludeProcessor> index,
        YamlIncludeResolver sut
    )
    {
        var processors = new List<(IYamlInclude Directive, IIncludeProcessor Value)>
        {
            (Substitute.ForPartsOf<TestYamlInclude1>(), Substitute.For<IIncludeProcessor>()),
            (Substitute.ForPartsOf<TestYamlInclude2>(), Substitute.For<IIncludeProcessor>()),
        };

        index.AddRange(processors.Select(x => (x.Directive.GetType(), x.Value)));

        processors[1]
            .Value.GetPathToConfig(default!, default!)
            .ReturnsForAnyArgs(_ =>
            {
                var fileInfo = Substitute.For<IFileInfo>();
                fileInfo.Exists.Returns(false);
                return fileInfo;
            });

        var act = () => sut.GetIncludePath(processors[1].Directive, SupportedServices.Radarr);

        act.Should().Throw<YamlIncludeException>().WithMessage("*does not exist*");
    }
}

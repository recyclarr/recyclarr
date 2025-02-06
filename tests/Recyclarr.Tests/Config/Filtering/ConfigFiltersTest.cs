using Recyclarr.Config.Filtering;
using Recyclarr.Config.Parsing;
using Recyclarr.TestLibrary;
using Recyclarr.TrashGuide;

namespace Recyclarr.Tests.Config.Filtering;

[TestFixture]
public class ConfigFiltersTest : IntegrationTestFixture
{
    [Test]
    public void Filter_out_invalid_instances()
    {
        var sut = Resolve<InvalidInstancesFilter>();

        var config = new RadarrConfigYaml { BaseUrl = "http://localhost:7878", ApiKey = "" };

        var context = new FilterContext();

        var result = sut.Filter(
            new ConfigFilterCriteria { Instances = ["instance1"] },
            [new LoadedConfigYaml("instance1", SupportedServices.Radarr, config)],
            context
        );

        result.Should().BeEmpty();

        var subject = context
            .Results.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<InvalidInstancesFilterResult>()
            .Which.InvalidInstances.Should()
            .ContainSingle()
            .Subject;

        subject.InstanceName.Should().Be("instance1");
        subject.Failures.Should().NotBeEmpty();
    }

    [Test]
    public void Filter_out_split_instances()
    {
        var sut = Resolve<SplitInstancesFilter>();

        var context = new FilterContext();

        var result = sut.Filter(
            new ConfigFilterCriteria { Instances = ["instance1"] },
            [
                new LoadedConfigYaml(
                    "instance1",
                    SupportedServices.Radarr,
                    new RadarrConfigYaml { BaseUrl = "http://same" }
                ),
                new LoadedConfigYaml(
                    "instance2",
                    SupportedServices.Radarr,
                    new RadarrConfigYaml { BaseUrl = "http://same" }
                ),
            ],
            context
        );

        result.Should().BeEmpty();

        var subject = context
            .Results.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<SplitInstancesFilterResult>()
            .Which.SplitInstances.Should()
            .ContainSingle()
            .Subject;

        subject.BaseUrl.Should().Be("http://same");
        subject.InstanceNames.Should().BeEquivalentTo("instance1", "instance2");
    }

    [Test]
    public void Filter_out_non_existent_instances()
    {
        var sut = Resolve<NonExistentInstancesFilter>();

        var context = new FilterContext();
        LoadedConfigYaml[] yaml =
        [
            new(
                "instance1",
                SupportedServices.Radarr,
                new RadarrConfigYaml { BaseUrl = "http://myradarr.domain.com" }
            ),
        ];

        var result = sut.Filter(
            new ConfigFilterCriteria { Instances = ["instance_non_existent"] },
            yaml,
            context
        );

        result.Should().BeEquivalentTo(yaml);

        var subject = context
            .Results.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<NonExistentInstancesFilterResult>()
            .Which.NonExistentInstances.Should()
            .ContainSingle()
            .Subject;

        subject.Should().Be("instance_non_existent");
    }

    [Test]
    public void Filter_out_duplicate_instances()
    {
        var sut = Resolve<DuplicateInstancesFilter>();

        var context = new FilterContext();
        LoadedConfigYaml[] yaml =
        [
            new(
                "instance1",
                SupportedServices.Radarr,
                new RadarrConfigYaml { BaseUrl = "http://different2" }
            ),
            new(
                "instance1",
                SupportedServices.Sonarr,
                new RadarrConfigYaml { BaseUrl = "http://different1" }
            ),
        ];

        var result = sut.Filter(
            new ConfigFilterCriteria { Instances = ["instance1"] },
            yaml,
            context
        );

        result.Should().BeEmpty();

        context
            .Results.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<DuplicateInstancesFilterResult>()
            .Which.DuplicateInstances.Should()
            .BeEquivalentTo("instance1");
    }
}

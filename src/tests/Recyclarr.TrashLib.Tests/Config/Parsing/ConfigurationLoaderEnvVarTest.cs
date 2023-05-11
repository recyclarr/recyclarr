using Recyclarr.Common;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationLoaderEnvVarTest : TrashLibIntegrationFixture
{
    [Test]
    public void Test_successful_environment_variable_loading()
    {
        var env = Resolve<IEnvironment>();
        env.GetEnvironmentVariable("SONARR_API_KEY").Returns("the_api_key");
        env.GetEnvironmentVariable("SONARR_URL").Returns("http://the_url");

        var sut = Resolve<ConfigurationLoader>();

        const string testYml = @"
sonarr:
  instance:
    api_key: !env_var SONARR_API_KEY
    base_url: !env_var SONARR_URL http://sonarr:1233
";

        var config = sut.Load(testYml);

        config.Should().BeEquivalentTo(new[]
        {
            new
            {
                BaseUrl = new Uri("http://the_url"),
                ApiKey = "the_api_key"
            }
        });
    }

    [Test]
    public void Use_default_value_if_env_var_not_defined()
    {
        var sut = Resolve<ConfigurationLoader>();

        const string testYml = @"
sonarr:
  instance:
    base_url: !env_var SONARR_URL http://sonarr:1233
    api_key: value
";

        var config = sut.Load(testYml);
        config.Should().BeEquivalentTo(new[]
        {
            new
            {
                BaseUrl = new Uri("http://sonarr:1233")
            }
        });
    }

    [Test]
    public void Default_value_with_spaces_is_allowed()
    {
        var env = Resolve<IEnvironment>();
        env.GetEnvironmentVariable("SONARR_URL").Returns("");

        var sut = Resolve<ConfigurationLoader>();

        const string testYml = @"
sonarr:
  instance:
    base_url: !env_var SONARR_URL http://somevalue
    api_key: value
";

        var config = sut.Load(testYml);
        config.Should().BeEquivalentTo(new[]
        {
            new
            {
                BaseUrl = new Uri("http://somevalue")
            }
        });
    }

    [Test]
    public void Quotation_characters_are_stripped_from_default_value()
    {
        var env = Resolve<IEnvironment>();
        env.GetEnvironmentVariable("SONARR_URL").Returns("");

        var sut = Resolve<ConfigurationLoader>();

        const string testYml = @"
sonarr:
  instance:
    base_url: !env_var SONARR_URL ""http://theurl""
    api_key: !env_var SONARR_API 'the key'
";

        var config = sut.Load(testYml);
        config.Should().BeEquivalentTo(new[]
        {
            new
            {
                BaseUrl = new Uri("http://theurl"),
                ApiKey = "the key"
            }
        });
    }

    [Test]
    public void Multiple_spaces_between_default_and_env_var_work()
    {
        var sut = Resolve<ConfigurationLoader>();

        const string testYml = @"
sonarr:
  instance:
    base_url: !env_var SONARR_URL    http://somevalue
    api_key: value
";

        var config = sut.Load(testYml);
        config.Should().BeEquivalentTo(new[]
        {
            new
            {
                BaseUrl = new Uri("http://somevalue")
            }
        });
    }

    [Test]
    public void Tab_characters_are_stripped()
    {
        var sut = Resolve<ConfigurationLoader>();

        const string testYml = $@"
sonarr:
  instance:
    base_url: !env_var SONARR_URL {"\t"}http://somevalue
    api_key: value
";

        var config = sut.Load(testYml);
        config.Should().BeEquivalentTo(new[]
        {
            new
            {
                BaseUrl = new Uri("http://somevalue")
            }
        });
    }

    [Test]
    public void No_configs_returned_when_no_env_var_and_no_default()
    {
        var sut = Resolve<ConfigurationLoader>();

        const string testYml = @"
sonarr:
  instance:
    base_url: !env_var SONARR_URL
    api_key: value
";

        var result = sut.Load(testYml);
        result.Should().BeEmpty();
    }
}

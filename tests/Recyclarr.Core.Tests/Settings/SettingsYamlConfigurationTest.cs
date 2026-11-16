using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Recyclarr.Common;
using Recyclarr.Settings.Configuration;
using Recyclarr.Settings.Models;
using Recyclarr.TestLibrary;

namespace Recyclarr.Core.Tests.Settings;

internal sealed class SettingsYamlConfigurationTest
{
    private readonly MockFileSystem _fs = new();
    private readonly TestEnvironment _env = new();

    private IDirectoryInfo ConfigDirectory => _fs.CurrentDirectory().SubDirectory("config");

    private SettingsBindResult Load(string? yaml, string fileName = "settings.yml")
    {
        if (yaml is not null)
        {
            _fs.AddFile(ConfigDirectory.File(fileName), new MockFileData(yaml));
        }

        _fs.AddDirectory(ConfigDirectory);
        var builder = new ConfigurationBuilder();
        var report = builder.AddSettingsYaml(ConfigDirectory, _env.Configuration);
        return SettingsBinder.Bind(builder.Build(), report);
    }

    [Test]
    public void Missing_file_yields_defaults()
    {
        var result = Load(null);

        result.Diagnostics.Should().BeEmpty();
        result.Settings.Should().BeEquivalentTo(new RecyclarrSettings());
    }

    [Test]
    public void Empty_file_yields_defaults()
    {
        var result = Load("");

        result.Diagnostics.Should().BeEmpty();
        result.Settings.Should().BeEquivalentTo(new RecyclarrSettings());
    }

    [Test]
    public void Every_setting_binds()
    {
        var result = Load(
            """
            git_path: /usr/bin/git
            enable_ssl_certificate_validation: false
            log_janitor:
              max_files: 5
            notifications:
              verbosity: detailed
              apprise:
                mode: stateless
                base_url: http://apprise:8000
                key: the-key
                tags: tag1
                urls:
                  - discord://hook
            server:
              port: 9000
              bind_address: 0.0.0.0
              management:
                port: 9001
                metrics: false
              logging:
                format: json
                files: false
              schedule:
                enabled: false
                cron: 0 6 * * *
            resource_providers:
              - name: guides
                type: trash-guides
                clone_url: https://github.com/TRaSH-Guides/Guides.git
                reference: master
                replace_default: true
                cache_limit: 1GB
              - name: local-cfs
                type: custom-formats
                service: radarr
                path: /data/cfs
            """
        );

        result.Diagnostics.Should().BeEmpty();
        result
            .Settings.Should()
            .BeEquivalentTo(
                new RecyclarrSettings
                {
                    GitPath = "/usr/bin/git",
                    EnableSslCertificateValidation = false,
                    LogJanitor = new LogJanitorSettings { MaxFiles = 5 },
                    Notifications = new NotificationSettings
                    {
                        Verbosity = NotificationVerbosity.Detailed,
                        Apprise = new AppriseNotificationSettings
                        {
                            Mode = AppriseMode.Stateless,
                            BaseUrl = new Uri("http://apprise:8000"),
                            Key = "the-key",
                            Tags = "tag1",
                            Urls = ["discord://hook"],
                        },
                    },
                    Server = new ServerSettings
                    {
                        Port = 9000,
                        BindAddress = "0.0.0.0",
                        Management = new ManagementSettings { Port = 9001, Metrics = false },
                        Logging = new ServerLoggingSettings
                        {
                            Format = ServerLogFormat.Json,
                            Files = false,
                        },
                        Schedule = new ScheduleSettings { Enabled = false, Cron = "0 6 * * *" },
                    },
                    ResourceProviders =
                    [
                        new GitResourceProvider
                        {
                            Name = "guides",
                            Type = "trash-guides",
                            CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
                            Reference = "master",
                            ReplaceDefault = true,
                            CacheLimit = DataSize.FromGigabytes(1),
                        },
                        new LocalResourceProvider
                        {
                            Name = "local-cfs",
                            Type = "custom-formats",
                            Service = "radarr",
                            Path = "/data/cfs",
                        },
                    ],
                },
                o => o.PreferringRuntimeMemberTypes()
            );
    }

    [Test]
    public void Value_tags_resolve_like_configuration_files()
    {
        _env.Set("APPRISE_URL", "http://apprise:8000");
        _fs.AddFile(ConfigDirectory.File("secrets.yml"), new MockFileData("apprise_key: s3cret"));
        _fs.AddFile(ConfigDirectory.File("git-path.txt"), new MockFileData("/opt/git\n"));

        var result = Load(
            """
            git_path: !file git-path.txt
            notifications:
              apprise:
                mode: stateful
                base_url: !env_var APPRISE_URL http://localhost:8000
                key: !secret apprise_key
            """
        );

        result.Diagnostics.Should().BeEmpty();
        result.Settings.GitPath.Should().Be("/opt/git");
        result
            .Settings.Notifications.Apprise.Should()
            .BeEquivalentTo(new { BaseUrl = new Uri("http://apprise:8000"), Key = "s3cret" });
    }

    [Test]
    public void Env_var_tag_falls_back_to_its_default()
    {
        var result = Load("git_path: !env_var GIT_PATH /usr/bin/git");

        result.Settings.GitPath.Should().Be("/usr/bin/git");
    }

    [Test]
    public void Unresolvable_tag_is_an_error_with_its_position()
    {
        var result = Load("server:\n  bind_address: !secret missing\n");

        result
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Describe()
            .Should()
            .StartWith(
                "settings.yml line 2, column 3: 'server.bind_address': Secret used on line 2"
            );
    }

    [Test]
    public void Yaml_extension_is_read()
    {
        var result = Load("git_path: /git", "settings.yaml");

        result.Settings.GitPath.Should().Be("/git");
    }

    [Test]
    public void Invalid_yaml_reports_its_line_and_column()
    {
        var result = Load(
            """
            server:
              port: [1
            """
        );

        result
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Should()
            .Match<SettingsDiagnostic>(d =>
                d.Severity == SettingsDiagnosticSeverity.Error
                && d.Message.StartsWith("invalid YAML", StringComparison.Ordinal)
                && d.Position!.Line > 0
                && d.Position.Column > 0
            );
        result.Settings.Should().BeEquivalentTo(new RecyclarrSettings());
    }

    [TestCase("repository")]
    [TestCase("repositories")]
    public void Removed_key_is_an_error_with_the_upgrade_message(string key)
    {
        var result = Load($"git_path: /git\n{key}:\n  clone_url: http://x\n");

        result
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Describe()
            .Should()
            .Be(
                $"settings.yml line 2, column 1: The `{key}` setting has been removed. Use "
                    + "`resource_providers` instead. "
                    + "See: https://recyclarr.dev/guide/upgrade-guide/v8.0/#resource-providers"
            );
    }

    [Test]
    public void Unknown_nested_key_names_its_yaml_path_and_line()
    {
        var result = Load(
            """
            server:
              management:
                prot: 9001
            """
        );

        result
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Describe()
            .Should()
            .Be("settings.yml line 3, column 5: unknown setting 'server.management.prot'");
    }

    [Test]
    public void Key_in_a_different_case_is_unknown()
    {
        var result = Load("Git_Path: /git");

        result
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Message.Should()
            .Be("unknown setting 'Git_Path'");
    }

    [Test]
    public void Unknown_key_in_a_resource_provider_names_its_index()
    {
        var result = Load(
            """
            resource_providers:
              - name: local
                type: trash-guides
                path: /data
                clone_url_typo: x
            """
        );

        result
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Message.Should()
            .Be("unknown setting 'resource_providers[0].clone_url_typo'");
    }

    [Test]
    public void Resource_provider_without_a_location_is_an_error()
    {
        var result = Load(
            """
            resource_providers:
              - name: nowhere
                type: trash-guides
            """
        );

        result
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Describe()
            .Should()
            .Be(
                "settings.yml line 2, column 5: 'resource_providers[0]' must set exactly one of "
                    + "'clone_url' or 'path'"
            );
    }

    [TestCase("server:\n  port: abc\n", "'server.port' must be a whole number (got 'abc')")]
    [TestCase(
        "server:\n  management:\n    metrics: maybe\n",
        "'server.management.metrics' must be true or false (got 'maybe')"
    )]
    [TestCase(
        "server:\n  logging:\n    format: xml\n",
        "'server.logging.format' must be one of 'json', 'text' (got 'xml')"
    )]
    [TestCase("server: 5\n", "'server' must be a mapping of settings")]
    [TestCase("server:\n  port:\n    - 1\n", "'server.port' must be a single value")]
    [TestCase("resource_providers: x\n", "'resource_providers' must be a list")]
    public void Value_of_the_wrong_type_is_an_error(string yaml, string expected)
    {
        var result = Load(yaml);

        result.Diagnostics.Should().ContainSingle().Which.Message.Should().Be(expected);
        result.Settings.Should().BeEquivalentTo(new RecyclarrSettings());
    }

    [Test]
    public void Invalid_data_size_explains_the_format()
    {
        var result = Load(
            """
            resource_providers:
              - name: guides
                type: trash-guides
                clone_url: http://x
                cache_limit: lots
            """
        );

        result
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Message.Should()
            .StartWith("'resource_providers[0].cache_limit' is invalid: Invalid data size 'lots'");
    }

    [Test]
    public void Empty_section_keeps_its_defaults()
    {
        var result = Load("server:\nnotifications:\n");

        result.Diagnostics.Should().BeEmpty();
        result.Settings.Should().BeEquivalentTo(new RecyclarrSettings());
    }

    [Test]
    public void Both_yml_and_yaml_files_are_an_error()
    {
        _fs.AddFile(ConfigDirectory.File("settings.yaml"), new MockFileData(""));

        var result = Load("");

        result
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Severity.Should()
            .Be(SettingsDiagnosticSeverity.Error);
    }
}

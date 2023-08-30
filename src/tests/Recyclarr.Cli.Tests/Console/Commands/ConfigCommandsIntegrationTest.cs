using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Autofac;
using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashLib.Config.Listers;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.Cli.Tests.Console.Commands;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigCommandsIntegrationTest : CliIntegrationFixture
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        base.RegisterTypes(builder);
        builder.RegisterMockFor<IConfigTemplatesRepo>(x =>
        {
            x.Path.Returns(_ => Fs.CurrentDirectory());
        });
    }

    [Test]
    [SuppressMessage("Usage", "NS5000:Received check.", Justification =
        "See: https://github.com/nsubstitute/NSubstitute.Analyzers/issues/211")]
    public async Task Repo_update_is_called_on_config_list()
    {
        var repo = Resolve<IConfigTemplatesRepo>();

        // Create this to make ConfigTemplateGuideService happy. It tries to parse this file, but
        // it won't exist because we don't operate with real Git objects (so a clone never happens).
        Fs.AddFile(repo.Path.File("templates.json"), new MockFileData("{}"));

        var sut = Resolve<ConfigListCommand>();
        await sut.ExecuteAsync(default!, new ConfigListCommand.CliSettings
        {
            ListCategory = ConfigCategory.Templates
        });

        await repo.Received().Update();
    }

    [Test]
    [SuppressMessage("Usage", "NS5000:Received check.", Justification =
        "See: https://github.com/nsubstitute/NSubstitute.Analyzers/issues/211")]
    public async Task Repo_update_is_called_on_config_create()
    {
        var repo = Resolve<IConfigTemplatesRepo>();

        // Create this to make ConfigTemplateGuideService happy. It tries to parse this file, but
        // it won't exist because we don't operate with real Git objects (so a clone never happens).
        Fs.AddFile(repo.Path.File("templates.json"), new MockFileData("{}"));

        var sut = Resolve<ConfigCreateCommand>();
        await sut.ExecuteAsync(default!, new ConfigCreateCommand.CliSettings
        {
            TemplatesOption = new[] {"some-template"}
        });

        await repo.Received().Update();
    }
}

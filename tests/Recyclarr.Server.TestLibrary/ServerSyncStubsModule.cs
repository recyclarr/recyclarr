using System.Reactive.Linq;
using Autofac;
using NSubstitute;
using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.TestLibrary.Autofac;

namespace Recyclarr.Server.TestLibrary;

/// <summary>
/// Stubs the sync engine so server tests exercise the HTTP surface without triggering real
/// network-bound sync pipelines. Register this last so it overrides the production registrations.
/// </summary>
public sealed class ServerSyncStubsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterMockFor<ISyncOrchestrator>(m =>
            m.RunAsync(
                    Arg.Any<IReadOnlyList<IServiceConfiguration>>(),
                    Arg.Any<ISyncSettings>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Task.FromResult(ExitStatus.Succeeded))
        );

        builder.RegisterMockFor<ISyncRunScope>(m =>
        {
            m.Pipelines.Returns(Observable.Never<PipelineEvent>());
            m.Diagnostics.Returns(Observable.Never<SyncDiagnosticEvent>());
        });
    }
}

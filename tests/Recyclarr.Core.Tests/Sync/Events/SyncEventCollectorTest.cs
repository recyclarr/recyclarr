using Recyclarr.Sync;
using Recyclarr.Sync.Events;

namespace Recyclarr.Core.Tests.Sync.Events;

[TestFixture]
internal sealed class SyncEventCollectorTest
{
    private static (SyncEventCollector Collector, SyncContextSource Context) CreateCollector(
        SyncEventStorage storage
    )
    {
        var context = new SyncContextSource();
        var collector = new SyncEventCollector(Substitute.For<ILogger>(), storage, context);
        return (collector, context);
    }

    [Test]
    public void Events_from_different_instances_are_distinguishable()
    {
        var storage = new SyncEventStorage();
        var (collector, context) = CreateCollector(storage);

        context.SetInstance("sonarr-main");
        collector.AddError("error 1");

        context.SetInstance("radarr-4k");
        collector.AddError("error 2");

        var sonarrErrors = storage.Diagnostics.Where(e => e.InstanceName == "sonarr-main");
        sonarrErrors.Should().ContainSingle().Which.Message.Should().Be("error 1");

        collector.Dispose();
    }

    [Test]
    public void Events_from_different_pipelines_are_distinguishable()
    {
        var storage = new SyncEventStorage();
        var (collector, context) = CreateCollector(storage);

        context.SetInstance("sonarr-main");
        context.SetPipeline(PipelineType.CustomFormat);
        collector.AddWarning("cf warning");

        context.SetPipeline(PipelineType.QualityProfile);
        collector.AddWarning("qp warning");

        var cfEvent = storage.Diagnostics.Single(e => e.Pipeline == PipelineType.CustomFormat);
        cfEvent.Message.Should().Be("cf warning");
        cfEvent.InstanceName.Should().Be("sonarr-main");

        collector.Dispose();
    }

    [Test]
    public void Changing_context_does_not_affect_previously_added_events()
    {
        var storage = new SyncEventStorage();
        var (collector, context) = CreateCollector(storage);

        context.SetInstance("instance-1");
        context.SetPipeline(PipelineType.CustomFormat);
        collector.AddWarning("first warning");

        context.SetInstance("instance-2");
        context.SetPipeline(PipelineType.QualitySize);
        collector.AddWarning("second warning");

        var firstEvent = storage.Diagnostics[0];
        firstEvent.InstanceName.Should().Be("instance-1");
        firstEvent.Pipeline.Should().Be(PipelineType.CustomFormat);
        firstEvent.Message.Should().Be("first warning");

        collector.Dispose();
    }

    [Test]
    public void All_diagnostic_types_accumulate_in_storage()
    {
        var storage = new SyncEventStorage();
        var (collector, context) = CreateCollector(storage);

        context.SetInstance("test");
        context.SetPipeline(PipelineType.MediaNaming);
        collector.AddError("an error");
        collector.AddWarning("a warning");
        collector.AddDeprecation("a deprecation");

        storage.Diagnostics.Should().HaveCount(3);
        storage
            .Diagnostics.Select(e => e.Type)
            .Should()
            .BeEquivalentTo([
                DiagnosticType.Error,
                DiagnosticType.Warning,
                DiagnosticType.Deprecation,
            ]);

        collector.Dispose();
    }

    [Test]
    public void Clear_removes_all_events()
    {
        var storage = new SyncEventStorage();
        var (collector, _) = CreateCollector(storage);

        collector.AddError("error");
        collector.AddWarning("warning");

        storage.Clear();

        storage.Diagnostics.Should().BeEmpty();

        collector.Dispose();
    }

    [Test]
    public void Events_without_context_have_null_instance_and_pipeline()
    {
        var storage = new SyncEventStorage();
        var (collector, _) = CreateCollector(storage);

        collector.AddWarning("no context");

        storage.Diagnostics[0].InstanceName.Should().BeNull();
        storage.Diagnostics[0].Pipeline.Should().BeNull();

        collector.Dispose();
    }

    [Test]
    public void Setting_instance_clears_pipeline()
    {
        var storage = new SyncEventStorage();
        var (collector, context) = CreateCollector(storage);

        context.SetInstance("instance-1");
        context.SetPipeline(PipelineType.CustomFormat);
        collector.AddWarning("with pipeline");

        // Setting new instance should clear pipeline
        context.SetInstance("instance-2");
        collector.AddWarning("without pipeline");

        storage.Diagnostics[0].Pipeline.Should().Be(PipelineType.CustomFormat);
        storage.Diagnostics[1].Pipeline.Should().BeNull();

        collector.Dispose();
    }
}

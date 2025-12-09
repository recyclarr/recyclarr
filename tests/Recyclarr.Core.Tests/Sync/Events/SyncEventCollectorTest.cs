using Recyclarr.Sync;
using Recyclarr.Sync.Events;

namespace Recyclarr.Core.Tests.Sync.Events;

[TestFixture]
internal sealed class SyncEventCollectorTest
{
    private static SyncEventCollector CreateCollector(SyncEventStorage storage)
    {
        return new SyncEventCollector(Substitute.For<ILogger>(), storage);
    }

    [Test]
    public void Events_from_different_instances_are_distinguishable()
    {
        var storage = new SyncEventStorage();
        var collector = CreateCollector(storage);

        using (collector.SetInstance("sonarr-main"))
        {
            collector.AddError("error 1");
        }

        using (collector.SetInstance("radarr-4k"))
        {
            collector.AddError("error 2");
        }

        var sonarrErrors = storage.Diagnostics.Where(e => e.InstanceName == "sonarr-main");

        sonarrErrors.Should().ContainSingle().Which.Message.Should().Be("error 1");
    }

    [Test]
    public void Events_from_different_pipelines_are_distinguishable()
    {
        var storage = new SyncEventStorage();
        var collector = CreateCollector(storage);

        using (collector.SetInstance("sonarr-main"))
        {
            using (collector.SetPipeline(PipelineType.CustomFormat))
            {
                collector.AddCompletionCount(185);
            }

            using (collector.SetPipeline(PipelineType.QualityProfile))
            {
                collector.AddCompletionCount(5);
            }
        }

        var cfEvent = storage.Completions.Single(e => e.Pipeline == PipelineType.CustomFormat);

        cfEvent.Count.Should().Be(185);
        cfEvent.InstanceName.Should().Be("sonarr-main");
    }

    [Test]
    public void Changing_context_does_not_affect_previously_added_events()
    {
        var storage = new SyncEventStorage();
        var collector = CreateCollector(storage);

        using (collector.SetInstance("instance-1"))
        using (collector.SetPipeline(PipelineType.CustomFormat))
        {
            collector.AddWarning("first warning");
        }

        using (collector.SetInstance("instance-2"))
        using (collector.SetPipeline(PipelineType.QualitySize))
        {
            collector.AddWarning("second warning");
        }

        var firstEvent = storage.Diagnostics[0];
        firstEvent.InstanceName.Should().Be("instance-1");
        firstEvent.Pipeline.Should().Be(PipelineType.CustomFormat);
        firstEvent.Message.Should().Be("first warning");
    }

    [Test]
    public void All_diagnostic_types_accumulate_in_storage()
    {
        var storage = new SyncEventStorage();
        var collector = CreateCollector(storage);

        using (collector.SetInstance("test"))
        using (collector.SetPipeline(PipelineType.MediaNaming))
        {
            collector.AddError("an error");
            collector.AddWarning("a warning");
            collector.AddDeprecation("a deprecation");
        }

        storage.Diagnostics.Should().HaveCount(3);
        storage
            .Diagnostics.Select(e => e.Type)
            .Should()
            .BeEquivalentTo([
                DiagnosticType.Error,
                DiagnosticType.Warning,
                DiagnosticType.Deprecation,
            ]);
    }

    [Test]
    public void Clear_removes_all_events()
    {
        var storage = new SyncEventStorage();
        var collector = CreateCollector(storage);

        collector.AddError("error");
        collector.AddWarning("warning");

        storage.Clear();

        storage.Diagnostics.Should().BeEmpty();
    }

    [Test]
    public void Scoped_context_clears_instance_on_dispose()
    {
        var storage = new SyncEventStorage();
        var collector = CreateCollector(storage);

        using (collector.SetInstance("test-instance"))
        {
            collector.AddWarning("inside scope");
        }

        collector.AddWarning("outside scope");

        storage.Diagnostics[0].InstanceName.Should().Be("test-instance");
        storage.Diagnostics[1].InstanceName.Should().BeNull();
    }

    [Test]
    public void Scoped_context_clears_pipeline_on_dispose()
    {
        var storage = new SyncEventStorage();
        var collector = CreateCollector(storage);

        using (collector.SetInstance("test"))
        {
            using (collector.SetPipeline(PipelineType.CustomFormat))
            {
                collector.AddWarning("inside pipeline scope");
            }

            collector.AddWarning("outside pipeline scope");
        }

        storage.Diagnostics[0].Pipeline.Should().Be(PipelineType.CustomFormat);
        storage.Diagnostics[1].Pipeline.Should().BeNull();
    }
}

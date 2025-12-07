using AwesomeAssertions;
using NUnit.Framework;
using Recyclarr.Sync;
using Recyclarr.Sync.Events;

namespace Recyclarr.Core.Tests.Sync.Events;

[TestFixture]
internal sealed class SyncEventCollectorTest
{
    [Test]
    public void Events_from_different_instances_are_distinguishable()
    {
        var storage = new SyncEventStorage();
        var collector = new SyncEventCollector(storage);

        collector.SetInstance("sonarr-main");
        collector.AddError("error 1");

        collector.SetInstance("radarr-4k");
        collector.AddError("error 2");

        var sonarrErrors = storage
            .Events.OfType<DiagnosticEvent>()
            .Where(e => e.InstanceName == "sonarr-main");

        sonarrErrors.Should().ContainSingle().Which.Message.Should().Be("error 1");
    }

    [Test]
    public void Events_from_different_pipelines_are_distinguishable()
    {
        var storage = new SyncEventStorage();
        var collector = new SyncEventCollector(storage);

        collector.SetInstance("sonarr-main");

        collector.SetPipeline(PipelineType.CustomFormat);
        collector.AddCompletionCount(185);

        collector.SetPipeline(PipelineType.QualityProfile);
        collector.AddCompletionCount(5);

        var cfEvent = storage
            .Events.OfType<CompletionEvent>()
            .Single(e => e.Pipeline == PipelineType.CustomFormat);

        cfEvent.Count.Should().Be(185);
        cfEvent.InstanceName.Should().Be("sonarr-main");
    }

    [Test]
    public void Changing_context_does_not_affect_previously_added_events()
    {
        var storage = new SyncEventStorage();
        var collector = new SyncEventCollector(storage);

        collector.SetInstance("instance-1");
        collector.SetPipeline(PipelineType.CustomFormat);
        collector.AddWarning("first warning");

        collector.SetInstance("instance-2");
        collector.SetPipeline(PipelineType.QualitySize);
        collector.AddWarning("second warning");

        var firstEvent = storage.Events.OfType<DiagnosticEvent>().First();
        firstEvent.InstanceName.Should().Be("instance-1");
        firstEvent.Pipeline.Should().Be(PipelineType.CustomFormat);
        firstEvent.Message.Should().Be("first warning");
    }

    [Test]
    public void All_diagnostic_types_accumulate_in_storage()
    {
        var storage = new SyncEventStorage();
        var collector = new SyncEventCollector(storage);

        collector.SetInstance("test");
        collector.SetPipeline(PipelineType.MediaNaming);

        collector.AddError("an error");
        collector.AddWarning("a warning");
        collector.AddDeprecation("a deprecation");

        storage.Events.Should().HaveCount(3);
        storage
            .Events.OfType<DiagnosticEvent>()
            .Select(e => e.Type)
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
        var collector = new SyncEventCollector(storage);

        collector.AddError("error");
        collector.AddWarning("warning");

        storage.Clear();

        storage.Events.Should().BeEmpty();
    }
}

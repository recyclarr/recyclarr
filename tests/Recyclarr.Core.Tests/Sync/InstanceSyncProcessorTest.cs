using System.Net;
using Autofac;
using NSubstitute.ExceptionExtensions;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;
using Refit;
using Serilog.Events;

namespace Recyclarr.Core.Tests.Sync;

internal sealed class InstanceSyncProcessorTest
{
    [Test]
    public async Task Handled_failure_is_retained_and_logged()
    {
        var exception = CreateApiRequestException();
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ThrowsAsync(exception);
        var log = new RecordingLogger();
        var pipelines = new RecordingPipelineExecutor();
        var sut = CreateSut(log, serviceInfo, pipelines);

        var result = await sut.Process(
            Substitute.For<ISyncSettings>(),
            CreateState(),
            CancellationToken.None
        );

        result.Failure.Should().BeOfType<ServiceUnavailableFailure>();
        log.Events.Where(evt => evt.Level == LogEventLevel.Debug)
            .Should()
            .ContainSingle()
            .Which.Exception.Should()
            .BeSameAs(exception);
    }

    [Test]
    public async Task Unexpected_failure_is_rethrown()
    {
        var exception = new InvalidOperationException("unexpected");
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ThrowsAsync(exception);
        var log = new RecordingLogger();
        var pipelines = new RecordingPipelineExecutor();
        var sut = CreateSut(log, serviceInfo, pipelines);

        var act = () =>
            sut.Process(Substitute.For<ISyncSettings>(), CreateState(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("unexpected");
        log.Events.Should().NotContain(evt => evt.Exception == exception);
    }

    [Test]
    public async Task Sync_state_failure_is_retained()
    {
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ReturnsForAnyArgs("Radarr");
        serviceInfo.GetVersion(default).ReturnsForAnyArgs(new Version(6, 0));
        var pipelines = new ThrowingPipelineExecutor(
            new SyncStateUnavailableException(new IOException("unavailable"))
        );
        var sut = CreateSut(Substitute.For<ILogger>(), serviceInfo, pipelines);

        var result = await sut.Process(
            Substitute.For<ISyncSettings>(),
            CreateState(),
            CancellationToken.None
        );

        result.Failure.Should().BeOfType<SyncStateUnavailableFailure>();
    }

    [TestCase(HttpStatusCode.Unauthorized, typeof(ServiceUnauthenticatedFailure))]
    [TestCase(HttpStatusCode.Forbidden, typeof(ServiceUnauthorizedFailure))]
    [TestCase(HttpStatusCode.TooManyRequests, typeof(ServiceRateLimitedFailure))]
    [TestCase(HttpStatusCode.InternalServerError, typeof(ServiceUnavailableFailure))]
    public async Task Http_failure_is_classified_without_response_details(
        HttpStatusCode statusCode,
        Type expectedType
    )
    {
        var exception = await CreateApiException(statusCode);
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ThrowsAsync(exception);
        var sut = CreateSut(
            Substitute.For<ILogger>(),
            serviceInfo,
            new RecordingPipelineExecutor()
        );

        var result = await sut.Process(
            Substitute.For<ISyncSettings>(),
            CreateState(),
            CancellationToken.None
        );

        result.Failure.Should().BeOfType(expectedType);
    }

    [Test]
    public async Task Incompatible_service_fails_instance_without_running_pipelines()
    {
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ReturnsForAnyArgs("Sonarr");
        serviceInfo.GetVersion(default).ReturnsForAnyArgs(new Version(3, 0));
        var sut = CreateSut(
            Substitute.For<ILogger>(),
            serviceInfo,
            new RecordingPipelineExecutor(),
            new Recyclarr.Config.Models.SonarrConfiguration { InstanceName = "instance" }
        );

        var result = await sut.Process(
            Substitute.For<ISyncSettings>(),
            CreateState(),
            CancellationToken.None
        );

        result.Failure.Should().BeOfType<ServiceIncompatibleFailure>();
    }

    private static InstanceSyncProcessor CreateSut(
        ILogger log,
        IServiceInformation serviceInfo,
        IPipelineExecutor pipelines,
        Recyclarr.Config.Models.IServiceConfiguration? config = null
    )
    {
        // Only the Sonarr test reaches this fetcher; it reports a version below the minimum.
        var capabilityFetcher = Substitute.For<ISonarrCapabilityFetcher>();
        capabilityFetcher
            .GetCapabilities(default)
            .ReturnsForAnyArgs(new SonarrCapabilities(new Version(3, 0)));
        var radarrFetcher = Substitute.For<IRadarrCapabilityFetcher>();
        radarrFetcher
            .GetCapabilities(default)
            .ReturnsForAnyArgs(new RadarrCapabilities(new Version(6, 0)));
        var enforcer = new ServiceAgnosticCapabilityEnforcer(
            serviceInfo,
            new SonarrCapabilityEnforcer(capabilityFetcher),
            new RadarrCapabilityEnforcer(radarrFetcher)
        );

        var builder = new ContainerBuilder();
        builder.RegisterInstance(new PlanBuilder(new PipelineGraph([]), log));
        builder.RegisterInstance(pipelines).As<IPipelineExecutor>();

        return new InstanceSyncProcessor(
            log,
            config ?? new Recyclarr.Config.Models.RadarrConfiguration { InstanceName = "instance" },
            enforcer,
            builder.Build()
        );
    }

    private static InstanceExecutionState CreateState() =>
        new(new Recyclarr.Config.Models.RadarrConfiguration { InstanceName = "instance" });

    private sealed class RecordingPipelineExecutor : IPipelineExecutor
    {
        public Task<IReadOnlyList<PipelineResult>> Execute(
            ISyncSettings settings,
            PipelinePlan plan,
            PipelineExecutionBuffer buffer,
            CancellationToken ct
        ) => throw new InvalidOperationException("Pipeline execution was not expected");
    }

    private sealed class ThrowingPipelineExecutor(Exception exception) : IPipelineExecutor
    {
        public Task<IReadOnlyList<PipelineResult>> Execute(
            ISyncSettings settings,
            PipelinePlan plan,
            PipelineExecutionBuffer buffer,
            CancellationToken ct
        ) => Task.FromException<IReadOnlyList<PipelineResult>>(exception);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEvent> Events { get; } = [];

        public void Write(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }

    // Built the way Refit builds them, without a server: only the exception type and status code
    // matter to the classifier under test.
    private static async Task<ApiException> CreateApiException(HttpStatusCode statusCode)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://service/");
        using var response = new HttpResponseMessage(statusCode) { RequestMessage = request };
        return await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());
    }

    private static ApiRequestException CreateApiRequestException()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://service/");
        return new ApiRequestException(
            request,
            HttpMethod.Get,
            new RefitSettings(),
            new HttpRequestException("Connection refused")
        );
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NSubstitute.ExceptionExtensions;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.ErrorHandling;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;
using Refit;
using Serilog.Events;

namespace Recyclarr.Core.Tests.Sync;

internal interface IFailureApi
{
    [Get("/")]
    Task<string> Get(CancellationToken ct);
}

internal sealed class InstanceSyncProcessorTest
{
    [Test]
    public async Task Handled_failure_is_retained_and_interrupts_pipelines()
    {
        var exception = await CreateApiRequestException();
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ThrowsAsync(exception);
        var failure = new HttpConnectionFailure();
        var strategy = Substitute.For<IExceptionStrategy>();
        strategy.HandleAsync(exception).Returns(failure);
        var log = new RecordingLogger();
        var publisher = new RecordingInstancePublisher();
        var pipelines = new RecordingPipelineExecutor();
        var sut = CreateSut(log, serviceInfo, publisher, pipelines, [strategy]);

        var result = await sut.Process(
            Substitute.For<ISyncSettings>(),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        result.Failure.Should().BeOfType<ServiceUnavailableFailure>();
        publisher.Outcomes.Should().ContainSingle().Which.Should().BeSameAs(failure);
        pipelines.Interrupted.Should().BeTrue();
        log.Events.Where(evt => evt.Level == LogEventLevel.Debug)
            .Should()
            .ContainSingle()
            .Which.Exception.Should()
            .BeSameAs(exception);
    }

    [Test]
    public async Task Unexpected_failure_is_rethrown_without_an_outcome()
    {
        var exception = new InvalidOperationException("unexpected");
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ThrowsAsync(exception);
        var log = new RecordingLogger();
        var publisher = new RecordingInstancePublisher();
        var pipelines = new RecordingPipelineExecutor();
        var sut = CreateSut(log, serviceInfo, publisher, pipelines, []);

        var act = () =>
            sut.Process(
                Substitute.For<ISyncSettings>(),
                new PipelineExecutionBuffer(),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("unexpected");
        publisher.Outcomes.Should().BeEmpty();
        pipelines.Interrupted.Should().BeFalse();
        log.Events.Should().NotContain(evt => evt.Exception == exception);
    }

    [Test]
    public async Task Sync_state_failure_is_retained_and_interrupts_pipelines()
    {
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ReturnsForAnyArgs("Radarr");
        serviceInfo.GetVersion(default).ReturnsForAnyArgs(new Version(6, 0));
        var pipelines = new ThrowingPipelineExecutor(
            new SyncStateUnavailableException(new IOException("unavailable"))
        );
        var sut = CreateSut(
            Substitute.For<ILogger>(),
            serviceInfo,
            new RecordingInstancePublisher(),
            pipelines,
            []
        );

        var result = await sut.Process(
            Substitute.For<ISyncSettings>(),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        result.Failure.Should().BeOfType<SyncStateUnavailableFailure>();
        pipelines.Interrupted.Should().BeTrue();
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
            new RecordingInstancePublisher(),
            new RecordingPipelineExecutor(),
            [new HttpExceptionStrategy()]
        );

        var result = await sut.Process(
            Substitute.For<ISyncSettings>(),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        result.Failure.Should().BeOfType(expectedType);
    }

    private static InstanceSyncProcessor CreateSut(
        ILogger log,
        IServiceInformation serviceInfo,
        IInstancePublisher publisher,
        IPipelineExecutor pipelines,
        IEnumerable<IExceptionStrategy> strategies
    )
    {
        IPlanComponent[] components = [];
        var planBuilder = new PlanBuilder(components.OrderBy(_ => 0), publisher, log);
        var enforcer = new ServiceAgnosticCapabilityEnforcer(
            serviceInfo,
            new SonarrCapabilityEnforcer(Substitute.For<ISonarrCapabilityFetcher>()),
            new RadarrCapabilityEnforcer(Substitute.For<IRadarrCapabilityFetcher>())
        );
        return new InstanceSyncProcessor(
            log,
            new Recyclarr.Config.Models.RadarrConfiguration { InstanceName = "instance" },
            publisher,
            planBuilder,
            pipelines,
            enforcer,
            strategies
        );
    }

    private sealed class RecordingInstancePublisher : IInstancePublisher
    {
        public List<SyncOutcome> Outcomes { get; } = [];

        public void Add(SyncOutcome outcome) => Outcomes.Add(outcome);

        public void AddError(string message) { }

        public void AddWarning(string message) { }

        public void AddDeprecation(string message) { }

        public IPipelinePublisher ForPipeline(PipelineType type) => IPipelinePublisher.Noop;
    }

    private sealed class RecordingPipelineExecutor : IPipelineExecutor
    {
        public bool Interrupted { get; private set; }

        public Task<IReadOnlyList<PipelineResult>> Execute(
            ISyncSettings settings,
            PipelinePlan plan,
            IInstancePublisher instancePublisher,
            PipelineExecutionBuffer buffer,
            CancellationToken ct
        ) => throw new InvalidOperationException("Pipeline execution was not expected");

        public void InterruptAll(IInstancePublisher instancePublisher)
        {
            Interrupted = true;
        }
    }

    private sealed class ThrowingPipelineExecutor(Exception exception) : IPipelineExecutor
    {
        public bool Interrupted { get; private set; }

        public Task<IReadOnlyList<PipelineResult>> Execute(
            ISyncSettings settings,
            PipelinePlan plan,
            IInstancePublisher instancePublisher,
            PipelineExecutionBuffer buffer,
            CancellationToken ct
        ) => Task.FromException<IReadOnlyList<PipelineResult>>(exception);

        public void InterruptAll(IInstancePublisher instancePublisher)
        {
            Interrupted = true;
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEvent> Events { get; } = [];

        public void Write(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }

    [SuppressMessage(
        "Reliability",
        "CA2025:Ensure tasks using IDisposable instances complete before the instances are disposed",
        Justification = "The server task completes before the listener is disposed in finally."
    )]
    private static async Task<ApiException> CreateApiException(HttpStatusCode statusCode)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        var server = Respond(listener, statusCode);
        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://127.0.0.1:{endpoint.Port}");
        var api = RestService.For<IFailureApi>(client);

        try
        {
            await api.Get(CancellationToken.None);
            throw new AssertionException("Expected Refit to reject the response.");
        }
        catch (ApiException e)
        {
            return e;
        }
        finally
        {
            await server;
            listener.Stop();
        }
    }

    private static async Task Respond(TcpListener listener, HttpStatusCode statusCode)
    {
        using var connection = await listener.AcceptTcpClientAsync();
        await using var stream = connection.GetStream();
        var response = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {(int)statusCode} {statusCode}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
        );
        await stream.WriteAsync(response);
    }

    [SuppressMessage(
        "Reliability",
        "CA2025:Ensure tasks using IDisposable instances complete before the instances are disposed",
        Justification = "The server task completes before the listener is disposed in finally."
    )]
    private static async Task<ApiRequestException> CreateApiRequestException()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        var server = CloseConnection(listener);
        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://127.0.0.1:{endpoint.Port}");
        var api = RestService.For<IFailureApi>(client);

        try
        {
            await api.Get(CancellationToken.None);
            throw new AssertionException("Expected Refit to reject the closed connection.");
        }
        catch (ApiRequestException e)
        {
            return e;
        }
        finally
        {
            await server;
            listener.Stop();
        }
    }

    private static async Task CloseConnection(TcpListener listener)
    {
        using var connection = await listener.AcceptTcpClientAsync();
        connection.Client.LingerState = new LingerOption(enable: true, seconds: 0);
    }
}

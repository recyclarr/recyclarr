using System.IO.Abstractions;
using Autofac;
using Recyclarr.Client.V1;
using Recyclarr.Server.TestLibrary;

namespace Recyclarr.Cli.Tests.Reusable;

/// <summary>
/// Runs Recyclarr.Server in-process and hands the test a real Refit client bound to it, so CLI
/// command handlers are exercised over the same HTTP surface they use in production instead of
/// against a substituted API.
/// </summary>
/// <remarks>
/// Two containers are in play: the server's, built by <see cref="ServerHttpFixture"/>, and the
/// CLI's, built by <see cref="CliIntegrationFixture"/>. They are deliberately separate, exactly
/// as they are in production, and share nothing but the HTTP connection between them.
/// </remarks>
internal abstract class CliServerHttpFixture : ServerHttpFixture
{
    // Refit needs an absolute base address. The handler behind it routes in-memory, so the
    // authority is never resolved.
    private static readonly Uri ServerAddress = new("http://localhost");

    private readonly CliContainer _cli;
    private readonly Lazy<ISyncApi> _api;

    protected CliServerHttpFixture()
    {
        _cli = new CliContainer(CreateClient);
        _api = new Lazy<ISyncApi>(() => _cli.Resolve<Func<Uri, ISyncApi>>()(ServerAddress));
    }

    /// <summary>
    /// The CLI's own <c>ISyncApi</c>, built by the production registration (and therefore the
    /// production Refit settings), talking to the in-process server.
    /// </summary>
    protected ISyncApi Api => _api.Value;

    /// <summary>
    /// Writes a config the server can load, so a sync request has something to act on.
    /// </summary>
    protected void AddInstanceConfig(string instanceName)
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                $"""
                radarr:
                  {instanceName}:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );
    }

    protected T ResolveCli<T>()
        where T : notnull
    {
        return _cli.Resolve<T>();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cli.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class CliContainer(Func<HttpClient> createClient) : CliIntegrationFixture
    {
        protected override void RegisterStubsAndMocks(ContainerBuilder builder)
        {
            base.RegisterStubsAndMocks(builder);

            builder.RegisterInstance(createClient);
        }

        public new T Resolve<T>()
            where T : notnull
        {
            return base.Resolve<T>();
        }
    }
}

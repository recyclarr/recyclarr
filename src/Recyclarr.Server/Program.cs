using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Server.Hosting;

var builder = ServerHost.CreateBuilder(
    args,
    new FileSystem(),
    EnvironmentConfiguration.FromProcess()
);
await using var app = ServerHost.Build(builder);
return app is null ? 1 : await app.RunServerAsync();

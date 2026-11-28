using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Server.Hosting;

var fileSystem = new FileSystem();
var environment = EnvironmentConfiguration.FromProcess();

// Build-time document generation runs this entry point with its own host arguments.
if (ServerLaunchOptions.IsDocumentGenerationProcess)
{
    return await RunAsync(new ServerLaunchOptions(), hostArgs: args);
}

return await ServerCommandLine.InvokeAsync(args, launch => RunAsync(launch));

async Task<int> RunAsync(ServerLaunchOptions launch, string[]? hostArgs = null)
{
    await using var app = ServerHost.Create(launch, fileSystem, environment, hostArgs: hostArgs);
    return app is null ? 1 : await app.RunServerAsync();
}

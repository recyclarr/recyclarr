global using Serilog;
// ASP.NET Core's implicit usings bring Microsoft.Extensions.Logging.ILogger into scope everywhere,
// which is ambiguous with Serilog's. Recyclarr logs through Serilog.
global using ILogger = Serilog.ILogger;

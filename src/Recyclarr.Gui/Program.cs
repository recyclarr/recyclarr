using System.IO.Abstractions;
using Autofac.Extensions.DependencyInjection;
using MudBlazor.Services;
using Recyclarr.Gui;
using Serilog;
using TrashLib.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

builder.Host
    .ConfigureServices(x => x.AddAutofac())
    .UseServiceProviderFactory(new AutofacServiceProviderFactory(CompositionRoot.Setup))
    .UseSerilog((_, provider, config) =>
    {
        var paths = provider.GetRequiredService<IAppPaths>();
        var logFile = paths.LogDirectory.SubDirectory("gui").File("gui.log");
        config
            .MinimumLevel.Debug()
            .WriteTo.File(logFile.FullName);
    });

var app = builder.Build();

var paths = app.Services.GetRequiredService<IAppPaths>();
var log = app.Services.GetRequiredService<ILogger>();
log.Debug("App Data Dir: {AppData}", paths.AppDataDirectory);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

using System.IO.Abstractions;
using Autofac;
using Blazored.LocalStorage;
using BlazorPro.BlazorSize;
using Fluxor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Recyclarr.Code.Radarr;
using Recyclarr.Code.Settings;
using Recyclarr.Code.Settings.Persisters;
using Serilog;
using TrashLib.Config;
using TrashLib.Radarr;
using TrashLib.Radarr.Config;

namespace Recyclarr
{
    internal static class CompositionRoot
    {
        public static void Build(IServiceCollection services, IConfiguration configuration)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddMediaQueryService();
            services.AddMudServices();
            services.AddBlazoredLocalStorage();
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddFluxor(options => options.ScanAssemblies(typeof(CompositionRoot).Assembly));

            // EFCore DB Context Factory Registrations
            services.AddDbContextFactory<RadarrDatabaseContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
        }

        public static void Build(ContainerBuilder builder)
        {
            // EF Core
            // builder.RegisterGeneric(typeof(DbContextFactory<>))
            //     .As(typeof(IDbContextFactory<>))
            //     .SingleInstance();

            builder.Register(_ => new LoggerConfiguration().MinimumLevel.Debug().CreateLogger())
                .As<ILogger>()
                .SingleInstance();

            builder.RegisterType<ResizeListener>().As<IResizeListener>();
            builder.RegisterType<FileSystem>().As<IFileSystem>();
            builder.RegisterType<ResourcePaths>().As<IResourcePaths>();

            // Persisters
            builder.RegisterType<SettingsPersister>().As<ISettingsPersister>();
            builder.RegisterType<AppSettingsPersister>().As<IAppSettingsPersister>();

            SetupRadarrDependencies(builder);
        }

        private static void SetupRadarrDependencies(ContainerBuilder builder)
        {
            builder.RegisterModule<RadarrAutofacModule>();

            builder.RegisterType<GuideProcessor>().As<IGuideProcessor>();
            builder.RegisterType<RadarrDatabaseContext>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RadarrConfigRepository>()
                .As<IConfigRepository<RadarrConfig>>()
                .InstancePerLifetimeScope();
        }
    }
}

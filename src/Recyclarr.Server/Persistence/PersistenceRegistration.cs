using Recyclarr.Server.Sync;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace Recyclarr.Server.Persistence;

internal static class PersistenceRegistration
{
    // MapTicker writes a process-wide TickerQ dictionary without locking
    // (TickerFunctionProvider.RegisterTypeMapping), which corrupts it when several hosts register
    // at once, as parallel in-process test servers do.
    private static readonly Lock TickerRegistrationLock = new();

    extension(IServiceCollection services)
    {
        // The context factory is an Autofac registration (CompositionRoot).
        public void AddServerPersistence()
        {
            services.AddTickerQ(options =>
                options.AddOperationalStore(ef =>
                    // TickerQueueDbContext applies TickerQ's entity configurations itself.
                    ef.UseApplicationDbContext<TickerQueueDbContext>(
                        ConfigurationType.IgnoreModelCustomizer
                    )
                )
            );
            lock (TickerRegistrationLock)
            {
                services.MapTicker<SyncJobTickerFunction, SyncJobTickerRequest>();
            }
        }
    }
}

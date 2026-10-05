using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace Recyclarr.Server.Persistence;

internal static class PersistenceRegistration
{
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
        }
    }
}

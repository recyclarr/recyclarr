using Autofac;
using Serilog;
using Serilog.Core;
using Module = Autofac.Module;

namespace Recyclarr.Logging;

public class LoggingAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<LoggingLevelSwitch>().SingleInstance();
        builder.RegisterType<LoggerFactory>();
        builder.Register(c => c.Resolve<LoggerFactory>().Create()).As<ILogger>().SingleInstance();
    }
}

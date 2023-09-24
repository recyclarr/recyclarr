using Autofac;

namespace Recyclarr.Settings;

public class SettingsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<SettingsProvider>().As<ISettingsProvider>().SingleInstance();
    }
}

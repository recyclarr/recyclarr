using Autofac;

namespace Recyclarr.Settings;

public class SettingsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<SettingsLoader>();
        builder.Register(c => c.Resolve<SettingsLoader>().LoadAndOptionallyCreate()).SingleInstance();
    }
}

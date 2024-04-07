using Autofac;

namespace Recyclarr.Settings;

public class SettingsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<SettingsLoader>();
        builder.RegisterType<SettingsProvider>().SingleInstance();

        builder.RegisterSettings(x => x);
        builder.RegisterSettings(x => x.LogJanitor);
        builder.RegisterSettings(x => x.Repositories.ConfigTemplates);
        builder.RegisterSettings(x => x.Repositories.TrashGuides);
        builder.RegisterSettings(x => x.Notifications);
    }
}

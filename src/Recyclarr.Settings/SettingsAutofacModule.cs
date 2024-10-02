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
    }
}

internal static class SettingsExtensions
{
    public static void RegisterSettings<TSettings>(
        this ContainerBuilder builder,
        Func<RecyclarrSettings, TSettings> settingsSelector)
    {
        builder.Register(c =>
            {
                var provider = c.Resolve<SettingsProvider>();
                var settings = settingsSelector(provider.Settings);
                return new Settings<TSettings>(settings);
            })
            .As<ISettings<TSettings>>()
            .SingleInstance();
    }
}

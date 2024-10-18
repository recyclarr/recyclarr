using Autofac;

namespace Recyclarr.Settings;

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

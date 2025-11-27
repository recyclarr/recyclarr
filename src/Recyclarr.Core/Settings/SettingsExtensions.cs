using Autofac;
using Recyclarr.Settings.Models;

namespace Recyclarr.Settings;

internal static class SettingsExtensions
{
    public static void RegisterSettings<TSettings>(
        this ContainerBuilder builder,
        Func<RecyclarrSettings, TSettings> settingsSelector
    )
    {
        builder
            .Register(c =>
            {
                var provider = c.Resolve<SettingsProvider>();
                return new Settings<TSettings>(() => settingsSelector(provider.Settings));
            })
            .As<ISettings<TSettings>>()
            .SingleInstance();
    }
}

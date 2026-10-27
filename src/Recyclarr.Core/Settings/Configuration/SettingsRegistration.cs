using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Recyclarr.Settings.Models;

namespace Recyclarr.Settings.Configuration;

public static class SettingsRegistration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <c>IOptions</c> for <see cref="RecyclarrSettings"/> and its sections, bound
        /// from <paramref name="settings"/> (a configuration holding only <c>settings.yml</c>)
        /// by <see cref="SettingsBinder"/>.
        /// </summary>
        /// <remarks>
        /// Settings problems never throw while values are created; the options validation of
        /// <see cref="RecyclarrSettings"/> reports them, and <c>ValidateOnStart</c> makes the host
        /// fail to start with them. Until then, values are the defaults.
        /// </remarks>
        public void AddRecyclarrSettings(IConfiguration settings, SettingsLoadReport report)
        {
            services.AddSingleton(report);
            services.AddSingleton(_ => SettingsBinder.Bind(settings, report));
            services.AddSingleton<IValidateOptions<RecyclarrSettings>, SettingsValidation>();
            services.AddOptions<RecyclarrSettings>().ValidateOnStart();

            AddSection(services, s => s);
            AddSection(services, s => s.Server);
            AddSection(services, s => s.Notifications);
            AddSection(services, s => s.LogJanitor);
        }
    }

    private static void AddSection<T>(
        IServiceCollection services,
        Func<RecyclarrSettings, T> select
    )
        where T : class
    {
        services.AddOptions<T>();
        services.AddSingleton<IOptionsFactory<T>>(sp => new SectionFactory<T>(
            sp.GetRequiredService<SettingsBindResult>(),
            select,
            sp.GetServices<IConfigureOptions<T>>(),
            sp.GetServices<IPostConfigureOptions<T>>(),
            sp.GetServices<IValidateOptions<T>>()
        ));
    }

    private sealed class SectionFactory<T>(
        SettingsBindResult result,
        Func<RecyclarrSettings, T> select,
        IEnumerable<IConfigureOptions<T>> setups,
        IEnumerable<IPostConfigureOptions<T>> postConfigures,
        IEnumerable<IValidateOptions<T>> validations
    ) : OptionsFactory<T>(setups, postConfigures, validations)
        where T : class
    {
        protected override T CreateInstance(string name)
        {
            return select(result.Settings);
        }
    }

    /// <summary>
    /// Fails with every settings error: load, key, and type problems from
    /// <see cref="SettingsBinder"/>, then the value rules of
    /// <see cref="RecyclarrSettingsValidator"/>, each with its YAML position when known.
    /// </summary>
    private sealed class SettingsValidation(SettingsBindResult result, SettingsLoadReport report)
        : IValidateOptions<RecyclarrSettings>
    {
        public ValidateOptionsResult Validate(string? name, RecyclarrSettings options)
        {
            var failures = result
                .Diagnostics.Where(d => d.Severity == SettingsDiagnosticSeverity.Error)
                .Select(d => d.Describe())
                .ToList();

            if (failures.Count == 0)
            {
                failures.AddRange(
                    new RecyclarrSettingsValidator()
                        .Validate(options)
                        .Errors.Select(e => Describe(e.PropertyName, e.ErrorMessage))
                );
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }

        // Validator property paths (ResourceProviders[0].Name) become configuration paths. A
        // value that is missing from the file has no position, so its nearest parent's is used.
        private string Describe(string propertyName, string message)
        {
            var path = ConfigurationPath.Combine(
                SettingsConfiguration.SectionName,
                propertyName
                    .Replace("[", ".", StringComparison.Ordinal)
                    .Replace("]", "", StringComparison.Ordinal)
                    .Replace('.', ':')
            );

            SettingsPosition? position = null;
            while (position is null && path.Length > 0)
            {
                position = report.Find(path);
                path = ConfigurationPath.GetParentPath(path) ?? "";
            }

            return new SettingsDiagnostic
            {
                Severity = SettingsDiagnosticSeverity.Error,
                File = report.FileName,
                Message = message,
                Position = position,
            }.Describe();
        }
    }
}

using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Recyclarr.Settings.Models;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.Settings.Configuration;

/// <summary>
/// The settings and every problem found while producing them. With any error, the settings are
/// the defaults.
/// </summary>
public sealed record SettingsBindResult(
    RecyclarrSettings Settings,
    IReadOnlyList<SettingsDiagnostic> Diagnostics
);

/// <summary>
/// Produces <see cref="RecyclarrSettings"/> from the <c>settings.yml</c> configuration section and
/// applies the product rules the parser does not know: removed keys, unknown keys, value types,
/// and the resource provider type chosen by its keys.
/// </summary>
/// <remarks>
/// The only way to turn settings configuration into settings; nothing else binds the section.
/// Rules run over the configuration keys before binding so every message can name the YAML path
/// and line; the binder's own unknown-key check stays on as a backstop. Keys must use the exact
/// snake_case name of a property. Enum values match member names without regard to case, which
/// covers every current settings enum; a multi-word enum member would need its snake_case form
/// mapped here.
/// </remarks>
public static class SettingsBinder
{
    private static readonly Dictionary<
        string,
        (string Message, SettingsDiagnosticSeverity)
    > RemovedKeys = new(StringComparer.Ordinal)
    {
        ["repositories"] = (
            "The `repositories` setting has been removed. Use `resource_providers` instead. "
                + "See: https://recyclarr.dev/guide/upgrade-guide/v8.0/#resource-providers",
            SettingsDiagnosticSeverity.Error
        ),
        ["repository"] = (
            "The `repository` setting has been removed. Use `resource_providers` instead. "
                + "See: https://recyclarr.dev/guide/upgrade-guide/v8.0/#resource-providers",
            SettingsDiagnosticSeverity.Error
        ),
    };

    private static readonly Action<BinderOptions> Strict = o =>
        o.ErrorOnUnknownConfiguration = true;

    public static SettingsBindResult Bind(IConfiguration configuration, SettingsLoadReport report)
    {
        var diagnostics = new List<SettingsDiagnostic>(report.Diagnostics);
        var section = configuration.GetSection(SettingsConfiguration.SectionName);
        new KeyRules(report, diagnostics).Object(section, typeof(RecyclarrSettings));

        if (diagnostics.Any(d => d.Severity == SettingsDiagnosticSeverity.Error))
        {
            return new SettingsBindResult(new RecyclarrSettings(), diagnostics);
        }

        try
        {
            return new SettingsBindResult(BindSettings(section), diagnostics);
        }
        catch (InvalidOperationException e)
        {
            diagnostics.Add(report.Error(e.Message));
            return new SettingsBindResult(new RecyclarrSettings(), diagnostics);
        }
    }

    /// <summary>
    /// The settings, or the defaults when they are invalid. For code that runs while a host is
    /// assembled, before problems can be reported; startup reports them later.
    /// </summary>
    public static RecyclarrSettings BindOrDefault(
        IConfiguration configuration,
        SettingsLoadReport report
    )
    {
        return Bind(configuration, report).Settings;
    }

    // Each property binds separately because the binder cannot create the abstract provider type.
    // A new top-level setting must be added here.
    private static RecyclarrSettings BindSettings(IConfigurationSection section)
    {
        var defaults = new RecyclarrSettings();
        return new RecyclarrSettings
        {
            ResourceProviders = section
                .GetSection(nameof(RecyclarrSettings.ResourceProviders))
                .GetChildren()
                .Select(BindProvider)
                .ToList(),
            EnableSslCertificateValidation =
                section
                    .GetSection(nameof(RecyclarrSettings.EnableSslCertificateValidation))
                    .Get<bool?>()
                ?? defaults.EnableSslCertificateValidation,
            LogJanitor = Get(section, nameof(RecyclarrSettings.LogJanitor), defaults.LogJanitor),
            GitPath = section[nameof(RecyclarrSettings.GitPath)],
            Notifications = Get(
                section,
                nameof(RecyclarrSettings.Notifications),
                defaults.Notifications
            ),
            Server = Get(section, nameof(RecyclarrSettings.Server), defaults.Server),
        };
    }

    private static T Get<T>(IConfigurationSection parent, string name, T fallback)
    {
        return parent.GetSection(name).Get<T>(Strict) ?? fallback;
    }

    private static ResourceProvider BindProvider(IConfigurationSection item)
    {
        var type =
            ProviderType(item)
            ?? throw new InvalidOperationException($"No provider type for '{item.Path}'");
        return (ResourceProvider)(
            item.Get(type, Strict)
            ?? throw new InvalidOperationException($"Empty provider at '{item.Path}'")
        );
    }

    private static Type? ProviderType(IConfigurationSection item)
    {
        var git = item.GetSection(nameof(GitResourceProvider.CloneUrl)).Exists();
        var local = item.GetSection(nameof(LocalResourceProvider.Path)).Exists();
        return (git, local) switch
        {
            (true, false) => typeof(GitResourceProvider),
            (false, true) => typeof(LocalResourceProvider),
            _ => null,
        };
    }

    private static string SnakeCase(string name)
    {
        return UnderscoredNamingConvention.Instance.Apply(name);
    }

    /// <summary>
    /// Checks configuration keys against the settings model and reports each problem with its
    /// YAML position.
    /// </summary>
    private sealed class KeyRules(SettingsLoadReport report, List<SettingsDiagnostic> diagnostics)
    {
        public void Object(IConfigurationSection section, Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => SnakeCase(p.Name), StringComparer.Ordinal);

            foreach (var child in section.GetChildren())
            {
                var position = report.Find(child.Path);
                var key = position?.YamlKey ?? child.Key;

                if (RemovedKeys.TryGetValue(key, out var removed))
                {
                    Add(removed.Item2, removed.Message, position);
                    continue;
                }

                if (!properties.TryGetValue(key, out var property))
                {
                    Error($"unknown setting '{position?.YamlPath ?? key}'", position);
                    continue;
                }

                Value(child, property.PropertyType);
            }
        }

        private void Value(IConfigurationSection section, Type declaredType)
        {
            var type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            var position = report.Find(section.Path);
            var kind = position?.Kind ?? SettingsNodeKind.Scalar;
            var name = position?.YamlPath ?? section.Path;

            // An empty value leaves the default in place.
            if (section.Value is null && kind == SettingsNodeKind.Scalar)
            {
                return;
            }

            if (IsScalar(type))
            {
                if (kind != SettingsNodeKind.Scalar)
                {
                    Error($"'{name}' must be a single value", position);
                    return;
                }

                Convert(section.Value!, type, name, position);
                return;
            }

            if (ElementType(type) is { } elementType)
            {
                if (kind != SettingsNodeKind.Sequence)
                {
                    Error($"'{name}' must be a list", position);
                    return;
                }

                foreach (var item in section.GetChildren())
                {
                    Element(item, elementType);
                }

                return;
            }

            if (kind != SettingsNodeKind.Mapping)
            {
                Error($"'{name}' must be a mapping of settings", position);
                return;
            }

            Object(section, type);
        }

        private void Element(IConfigurationSection item, Type elementType)
        {
            if (elementType != typeof(ResourceProvider))
            {
                Value(item, elementType);
                return;
            }

            var position = report.Find(item.Path);
            if (position?.Kind != SettingsNodeKind.Mapping)
            {
                Error(
                    $"'{position?.YamlPath ?? item.Path}' must be a mapping of settings",
                    position
                );
                return;
            }

            if (ProviderType(item) is not { } providerType)
            {
                Error(
                    $"'{position.YamlPath}' must set exactly one of 'clone_url' or 'path'",
                    position
                );
                return;
            }

            Object(item, providerType);
        }

        private void Convert(string value, Type type, string name, SettingsPosition? position)
        {
            if (type == typeof(string))
            {
                return;
            }

            try
            {
                TypeDescriptor.GetConverter(type).ConvertFromInvariantString(value);
            }
            catch (Exception e)
                when (e is FormatException or NotSupportedException or ArgumentException)
            {
                Error($"'{name}' {Expectation(type, e)} (got '{value}')", position);
            }
        }

        private static string Expectation(Type type, Exception e)
        {
            if (type.IsEnum)
            {
                var names = Enum.GetNames(type).Select(n => $"'{SnakeCase(n)}'");
                return $"must be one of {string.Join(", ", names)}";
            }

            if (type == typeof(bool))
            {
                return "must be true or false";
            }

            if (type == typeof(int) || type == typeof(long))
            {
                return "must be a whole number";
            }

            if (type == typeof(Uri))
            {
                return "must be a URL";
            }

            return $"is invalid: {(e.InnerException ?? e).Message}";
        }

        private void Error(string message, SettingsPosition? position)
        {
            Add(SettingsDiagnosticSeverity.Error, message, position);
        }

        private void Add(
            SettingsDiagnosticSeverity severity,
            string message,
            SettingsPosition? position
        )
        {
            diagnostics.Add(
                new SettingsDiagnostic
                {
                    Severity = severity,
                    File = report.FileName,
                    Message = message,
                    Position = position,
                }
            );
        }
    }

    private static bool IsScalar(Type type)
    {
        return type == typeof(string)
            || type.IsPrimitive
            || type.IsEnum
            || TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));
    }

    private static Type? ElementType(Type type)
    {
        return type.GetInterfaces()
            .Append(type)
            .FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            )
            ?.GetGenericArguments()[0];
    }
}

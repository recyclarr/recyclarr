using System.Collections;
using Microsoft.Extensions.Configuration;

namespace Recyclarr.Platform;

/// <summary>
/// Exposes environment variables as configuration, the only way Recyclarr code reads them.
/// </summary>
/// <remarks>
/// Hosts pass the variables in rather than letting the provider read the process, so tests can
/// supply their own. Keys follow .NET's environment-variable provider: <c>__</c> becomes the
/// configuration key delimiter, a prefix is matched without regard to case and stripped, and keys
/// are case-insensitive. Values are read when the configuration loads or reloads.
/// </remarks>
public static class EnvironmentConfiguration
{
    /// <summary>
    /// A copy of the current process environment.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> FromProcess()
    {
        return Environment
            .GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string?)e.Value, StringComparer.Ordinal);
    }

    extension(IConfigurationBuilder builder)
    {
        /// <summary>
        /// Adds the variables whose names start with <paramref name="prefix"/>, with the prefix
        /// removed.
        /// </summary>
        public IConfigurationBuilder AddEnvironment(
            IReadOnlyDictionary<string, string?> variables,
            string prefix = ""
        )
        {
            return builder.Add(new Source(variables, prefix));
        }
    }

    private sealed class Source(IReadOnlyDictionary<string, string?> variables, string prefix)
        : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new Provider(variables, prefix);
        }
    }

    private sealed class Provider(IReadOnlyDictionary<string, string?> variables, string prefix)
        : ConfigurationProvider
    {
        public override void Load()
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in variables)
            {
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var key = name[prefix.Length..]
                    .Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
                data[key] = value;
            }

            Data = data;
        }
    }
}

using Microsoft.Extensions.Configuration;
using Recyclarr.Platform;

namespace Recyclarr.TestLibrary;

/// <summary>
/// An in-memory process environment. <see cref="Configuration"/> exposes it the way production
/// hosts do (<see cref="EnvironmentConfiguration"/>) and reflects every <see cref="Set"/>. Hosts
/// built from <see cref="Variables"/> read them when they start.
/// </summary>
public sealed class TestEnvironment
{
    private readonly Dictionary<string, string?> _variables = new(StringComparer.Ordinal);
    private readonly IConfigurationRoot _configuration;

    public TestEnvironment()
    {
        _configuration = new ConfigurationBuilder().AddEnvironment(_variables).Build();
    }

    public IReadOnlyDictionary<string, string?> Variables => _variables;

    public IConfiguration Configuration => _configuration;

    /// <summary>
    /// Sets a variable; null unsets it.
    /// </summary>
    public void Set(string name, string? value)
    {
        if (value is null)
        {
            _variables.Remove(name);
        }
        else
        {
            _variables[name] = value;
        }

        _configuration.Reload();
    }
}

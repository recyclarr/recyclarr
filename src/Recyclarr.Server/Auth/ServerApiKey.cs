using System.Security.Cryptography;
using System.Text;
using Recyclarr.Platform;

namespace Recyclarr.Server.Auth;

/// <summary>
/// The server API key: the secret every request under <c>/api</c> must present as a bearer token
/// (ADR-027). It comes only from <c>RECYCLARR_API_KEY</c>, in both server modes; an ephemeral
/// server receives a one-time key from the CLI that launched it.
/// </summary>
/// <remarks>
/// Resolving this fails startup when the variable is unset or empty, so the server never serves
/// its API without a key. The value is never exposed, so it cannot reach a log by accident.
/// </remarks>
internal sealed class ServerApiKey
{
    public const string Variable = "RECYCLARR_API_KEY";

    private readonly byte[] _key;

    public ServerApiKey(IEnvironment env)
    {
        var key = env.GetEnvironmentVariable(Variable);
        if (string.IsNullOrEmpty(key))
        {
            throw new FatalException(
                $"The {Variable} environment variable must be set to the API key that clients "
                    + "send to this server"
            );
        }

        _key = Encoding.UTF8.GetBytes(key);
    }

    /// <summary>
    /// Compares in constant time, so response timing reveals nothing about the key.
    /// </summary>
    public bool Matches(string candidate)
    {
        return CryptographicOperations.FixedTimeEquals(_key, Encoding.UTF8.GetBytes(candidate));
    }
}

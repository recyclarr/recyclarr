using System.Security.Cryptography;
using System.Text;
using Recyclarr.Platform;

namespace Recyclarr.Server.Api.Auth;

/// <summary>
/// The server API key: the secret every request under <c>/api</c> must present as a bearer token
/// (ADR-027). It comes from <c>RECYCLARR_API_KEY</c> or <c>RECYCLARR_API_KEY_FILE</c>, in both
/// server modes; an ephemeral server receives a one-time key from the CLI that launched it.
/// </summary>
/// <remarks>
/// Resolving this fails startup when no valid key is configured, so the server never serves its
/// API without one. The key is resolved again on every comparison, so a rotated key file applies
/// without a restart; if it later becomes invalid, every request is rejected. The value is never
/// exposed, so it cannot reach a log by accident.
/// </remarks>
internal sealed class ServerApiKey
{
    public const int MinimumLength = 32;

    private readonly ServerApiKeyLocator _locator;
    private readonly ILogger _log;

    public ServerApiKey(ServerApiKeyLocator locator, ILogger log)
    {
        _locator = locator;
        _log = log;

        try
        {
            Resolve();
        }
        catch (EnvironmentException e)
        {
            throw new FatalException(e.Message);
        }
    }

    /// <summary>
    /// Compares in constant time, so response timing reveals nothing about the key.
    /// </summary>
    public bool Matches(string candidate)
    {
        byte[] key;
        try
        {
            key = Resolve();
        }
        catch (EnvironmentException e)
        {
            _log.Error(e, "Rejecting API request because the server API key is invalid");
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(key, Encoding.UTF8.GetBytes(candidate));
    }

    private byte[] Resolve()
    {
        var key =
            _locator.Locate()
            ?? throw new EnvironmentException(
                $"Set {ServerApiKeyLocator.Variable} or {ServerApiKeyLocator.FileVariable} to the "
                    + "API key that clients send to this server"
            );

        if (key.Length < MinimumLength)
        {
            throw new EnvironmentException(
                $"The server API key must be at least {MinimumLength} characters; generate one "
                    + "with `openssl rand -hex 32`"
            );
        }

        return Encoding.UTF8.GetBytes(key);
    }
}

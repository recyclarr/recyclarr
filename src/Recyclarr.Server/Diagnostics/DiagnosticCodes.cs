using System.Reflection;
using Recyclarr.Server.Features.Diagnostics.List;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Server.Sync.Results;
using static Recyclarr.Server.Features.Sync.GetResults.OutcomeSeverity;

namespace Recyclarr.Server.Diagnostics;

/// <summary>
/// A diagnostic code as published in the API: its wire value, the severity every diagnostic with
/// this code carries, and the type of its data. Codes follow the same contract rules as outcome
/// codes (ADR-016).
/// </summary>
internal sealed class DiagnosticCode<TData>(string value, OutcomeSeverity severity) : IOutcomeCode
    where TData : class
{
    public string Value => value;
    public Type? DataType { get; } = typeof(TData);

    /// <summary>
    /// Creates a diagnostic whose ID combines this code with <paramref name="key"/>. The key names
    /// the thing the problem is about, such as a variable or provider; the same problem must
    /// always produce the same key, because the ID must stay stable while the problem lasts.
    /// </summary>
    public DiagnosticResponse Create(string key, string message, TData data) =>
        new()
        {
            Id = $"{value}:{key}",
            Code = value,
            Severity = severity,
            Message = message,
            Data = data,
        };
}

/// <summary>
/// Every diagnostic code the API publishes. The OpenAPI document reads the public static fields of
/// this class to map codes to data schemas, so each code must be declared here. Wire values are
/// literals for the reason given on <see cref="OutcomeCodes"/>.
/// </summary>
internal static class DiagnosticCodes
{
    public static readonly DiagnosticCode<EnvironmentVariableDiagnosticData> EnvironmentVariableRemoved =
        new("environmentVariableRemoved", Warning);

    public static readonly DiagnosticCode<SettingDiagnosticData> SettingDeprecated = new(
        "settingDeprecated",
        Warning
    );

    public static readonly DiagnosticCode<ConfigFileDiagnosticData> ConfigPropertyDeprecated = new(
        "configPropertyDeprecated",
        Warning
    );

    // The edit has no effect, so the configuration the user asked for is not the one that syncs.
    public static readonly DiagnosticCode<ConfigErrorsDiagnosticData> ConfigEditRejected = new(
        "configEditRejected",
        Error
    );

    public static readonly DiagnosticCode<ProviderErrorsDiagnosticData> GuideUpdateRejected = new(
        "guideUpdateRejected",
        Warning
    );

    public static readonly DiagnosticCode<ProviderUpdateDiagnosticData> ResourceProviderUpdateFailed =
        new("resourceProviderUpdateFailed", Warning);

    public static IEnumerable<IOutcomeCode> All =>
        typeof(DiagnosticCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(x => x.GetValue(null))
            .OfType<IOutcomeCode>();
}

// The data shapes of diagnostic codes. The OpenAPI document publishes them as schemas because no
// endpoint references them directly.

/// <summary>A Recyclarr 8 environment variable that is still set.</summary>
internal sealed record EnvironmentVariableDiagnosticData(string Variable);

/// <summary>
/// A setting in a settings file. `line` is absent when the setting has no single location.
/// </summary>
internal sealed record SettingDiagnosticData(string File, int? Line);

/// <summary>The configuration file the problem is in. Absent when it is not known.</summary>
internal sealed record ConfigFileDiagnosticData(string? File);

/// <summary>The configuration errors that made the change invalid.</summary>
internal sealed record ConfigErrorsDiagnosticData(IReadOnlyList<string> Errors);

/// <summary>
/// The resource provider whose update was held back, and the configuration errors the update
/// caused.
/// </summary>
internal sealed record ProviderErrorsDiagnosticData(string Provider, IReadOnlyList<string> Errors);

/// <summary>
/// The resource provider that could not be updated, and when its data last updated. `lastSuccessAt`
/// is absent when it never updated with its current source.
/// </summary>
internal sealed record ProviderUpdateDiagnosticData(string Provider, DateTimeOffset? LastSuccessAt);

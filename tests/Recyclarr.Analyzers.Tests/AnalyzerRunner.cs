using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Recyclarr.Analyzers.Tests;

/// <summary>
/// Compiles C# source against the test process's runtime, Serilog, and AwesomeAssertions, runs
/// one analyzer, and
/// returns the source text each diagnostic points at. Source that fails to compile throws, so a
/// broken test input cannot pass as "no diagnostics".
/// </summary>
internal static class AnalyzerRunner
{
    private static readonly MetadataReference[] References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(ILogger).Assembly.Location)
            .Append(typeof(AssertionExtensions).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path)),
    ];

    public static async Task<IReadOnlyList<string>> FlaggedText(
        DiagnosticAnalyzer analyzer,
        string source
    )
    {
        var compilation = CSharpCompilation.Create(
            "AnalyzerTestInput",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var errors = compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Test source does not compile:\n" + string.Join("\n", errors)
            );
        }

        var diagnostics = await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();
        return
        [
            .. diagnostics
                .OrderBy(d => d.Location.SourceSpan.Start)
                .Select(d =>
                    source.Substring(d.Location.SourceSpan.Start, d.Location.SourceSpan.Length)
                ),
        ];
    }
}

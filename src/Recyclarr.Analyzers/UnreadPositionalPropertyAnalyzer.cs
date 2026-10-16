using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Recyclarr.Analyzers;

/// <summary>
/// Reports positional record properties that no code reads; compiler-generated <c>Equals</c>,
/// <c>GetHashCode</c>, and <c>ToString</c> do not count. Only records nested in a private type
/// chain are checked, because other assemblies (including InternalsVisibleTo friends) may read
/// anything more visible. Reads through reflection, such as serialization or equivalency
/// assertions, are invisible to this rule.
/// </summary>
/// <remarks>
/// Reads are collected across the compilation and reported at compilation end, so the IDE shows
/// these diagnostics only after a full analysis pass, not while typing.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class UnreadPositionalPropertyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "REC004",
        "Positional record property is never read",
        "Positional property '{0}' is never read outside compiler-generated members",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var candidates = new ConcurrentDictionary<IPropertySymbol, byte>(
                SymbolEqualityComparer.Default
            );
            var reads = new ConcurrentDictionary<IPropertySymbol, byte>(
                SymbolEqualityComparer.Default
            );

            start.RegisterSymbolAction(
                ctx =>
                {
                    foreach (var property in PositionalProperties((INamedTypeSymbol)ctx.Symbol))
                    {
                        candidates.TryAdd(property, 0);
                    }
                },
                SymbolKind.NamedType
            );

            start.RegisterOperationAction(
                ctx => MarkReads(ctx.Operation, reads),
                OperationKind.PropertyReference,
                OperationKind.DeconstructionAssignment,
                OperationKind.RecursivePattern,
                OperationKind.Loop,
                OperationKind.Invocation
            );

            start.RegisterCompilationEndAction(ctx =>
            {
                foreach (var property in candidates.Keys.Where(p => !reads.ContainsKey(p)))
                {
                    ctx.ReportDiagnostic(
                        Diagnostic.Create(Rule, property.Locations[0], property.Name)
                    );
                }
            });
        });
    }

    private static IEnumerable<IPropertySymbol> PositionalProperties(INamedTypeSymbol type)
    {
        if (!type.IsRecord || !IsInPrivateTypeChain(type))
        {
            return [];
        }

        return type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is ParameterSyntax))
            .Where(p => !ImplementsInterfaceMember(type, p));
    }

    private static bool IsInPrivateTypeChain(INamedTypeSymbol type)
    {
        for (var t = type; t is not null; t = t.ContainingType)
        {
            if (t.DeclaredAccessibility == Accessibility.Private)
            {
                return true;
            }
        }

        return false;
    }

    // Callers read interface implementations through the interface.
    private static bool ImplementsInterfaceMember(INamedTypeSymbol type, IPropertySymbol property)
    {
        return type
            .AllInterfaces.SelectMany(i => i.GetMembers())
            .Any(m =>
                SymbolEqualityComparer.Default.Equals(
                    type.FindImplementationForInterfaceMember(m),
                    property
                )
            );
    }

    private static void MarkReads(
        IOperation operation,
        ConcurrentDictionary<IPropertySymbol, byte> reads
    )
    {
        switch (operation)
        {
            case IPropertyReferenceOperation reference when !IsAssignmentTarget(reference):
                reads.TryAdd(reference.Property.OriginalDefinition, 0);
                break;

            // Deconstruct reads every positional property, including nested tuple forms:
            // `var ((a, b), n) = (record, 3);`
            case IDeconstructionAssignmentOperation
            {
                Syntax: AssignmentExpressionSyntax syntax,
                SemanticModel: { } model,
            }:
                MarkDeconstruction(model.GetDeconstructionInfo(syntax), reads);
                break;

            // Deconstructing loop variable, sync or `await foreach`: `foreach (var (a, b) in xs)`
            case IForEachLoopOperation
            {
                Syntax: ForEachVariableStatementSyntax syntax,
                SemanticModel: { } model,
            }:
                MarkDeconstruction(model.GetDeconstructionInfo(syntax), reads);
                break;

            // Positional pattern: `record is (1, _)`
            case IRecursivePatternOperation { DeconstructSymbol: not null } pattern
                when pattern.MatchedType is INamedTypeSymbol type:
                MarkAll(type, reads);
                break;

            // Explicit call: `record.Deconstruct(out var a, out var b);`
            case IInvocationOperation
            {
                TargetMethod: { Name: "Deconstruct", ContainingType: { } type },
            }:
                MarkAll(type, reads);
                break;
        }
    }

    private static void MarkDeconstruction(
        DeconstructionInfo info,
        ConcurrentDictionary<IPropertySymbol, byte> reads
    )
    {
        if (info.Method is { Name: "Deconstruct", ContainingType: { } type })
        {
            MarkAll(type, reads);
        }

        foreach (var nested in info.Nested)
        {
            MarkDeconstruction(nested, reads);
        }
    }

    private static void MarkAll(
        INamedTypeSymbol type,
        ConcurrentDictionary<IPropertySymbol, byte> reads
    )
    {
        foreach (var property in PositionalProperties(type.OriginalDefinition))
        {
            reads.TryAdd(property, 0);
        }
    }

    private static bool IsAssignmentTarget(IPropertyReferenceOperation reference)
    {
        return reference.Parent is ISimpleAssignmentOperation assignment
            && assignment.Target == reference;
    }
}

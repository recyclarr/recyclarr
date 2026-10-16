using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Recyclarr.Analyzers;

/// <summary>
/// Reports explicit arguments equal to the optional parameter's declared default. Besides
/// compile-time constants, this covers <c>default</c> and <c>CancellationToken.None</c> passed
/// to a struct parameter declared <c>= default</c>. Two cases are skipped: named arguments, which
/// state the value on purpose (<c>isDefault: false</c> in a test about non-default groups), and
/// arguments followed by another positional argument, whose removal would shift later arguments.
/// Arguments whose removal would bind a different overload are also not reported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class RedundantDefaultArgumentAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "REC001",
        "Argument equals the parameter default",
        "Argument for '{0}' equals its default value; omit it",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var cancellationToken = start.Compilation.GetTypeByMetadataName(
                "System.Threading.CancellationToken"
            );
            start.RegisterOperationAction(
                ctx => Analyze(ctx, cancellationToken),
                OperationKind.Argument
            );
        });
    }

    private static void Analyze(OperationAnalysisContext ctx, INamedTypeSymbol? cancellationToken)
    {
        var argument = (IArgumentOperation)ctx.Operation;
        if (argument.ArgumentKind != ArgumentKind.Explicit)
        {
            return;
        }

        var parameter = argument.Parameter;
        if (
            parameter is not { HasExplicitDefaultValue: true }
            || argument.Syntax is not ArgumentSyntax { NameColon: null } syntax
            || IsFollowedByPositional(syntax)
        )
        {
            return;
        }

        if (
            !EqualsDefault(argument.Value, parameter, cancellationToken)
            || !OmittingKeepsTarget(argument, syntax)
        )
        {
            return;
        }

        ctx.ReportDiagnostic(
            Diagnostic.Create(Rule, argument.Syntax.GetLocation(), parameter.Name)
        );
    }

    // Rebinds the call without the argument; another overload may otherwise take its place.
    // Call forms that cannot be rebound (attributes, primary constructor base calls) are not
    // reported.
    private static bool OmittingKeepsTarget(IArgumentOperation argument, ArgumentSyntax syntax)
    {
        var target = argument.Parent switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IObjectCreationOperation creation => creation.Constructor,
            _ => null,
        };

        if (
            target is null
            || argument.SemanticModel is not { } model
            || syntax.Parent is not ArgumentListSyntax list
        )
        {
            return false;
        }

        var shorter = list.WithArguments(list.Arguments.Remove(syntax));
        var position = list.Parent?.SpanStart ?? list.SpanStart;
        var rebound = list.Parent switch
        {
            InvocationExpressionSyntax or ObjectCreationExpressionSyntax =>
                model.GetSpeculativeSymbolInfo(
                    position,
                    list.Parent.ReplaceNode(list, shorter),
                    SpeculativeBindingOption.BindAsExpression
                ),
            // Target-typed `new(...)` has no type in isolation; rebind as `new T(...)`
            ImplicitObjectCreationExpressionSyntax creation => model.GetSpeculativeSymbolInfo(
                position,
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.ParseTypeName(
                        target.ContainingType.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        )
                    ),
                    shorter,
                    creation.Initializer
                ),
                SpeculativeBindingOption.BindAsExpression
            ),
            ConstructorInitializerSyntax initializer => model.GetSpeculativeSymbolInfo(
                position,
                initializer.WithArgumentList(shorter)
            ),
            _ => default,
        };

        return rebound.Symbol is IMethodSymbol method
            && SymbolEqualityComparer.Default.Equals(Unreduced(method), Unreduced(target));
    }

    private static IMethodSymbol Unreduced(IMethodSymbol method)
    {
        return (method.ReducedFrom ?? method).OriginalDefinition;
    }

    private static bool IsFollowedByPositional(ArgumentSyntax syntax)
    {
        if (syntax.Parent is not BaseArgumentListSyntax list)
        {
            return false;
        }

        return list.Arguments.SkipWhile(a => a != syntax).Skip(1).Any(a => a.NameColon is null);
    }

    private static bool EqualsDefault(
        IOperation value,
        IParameterSymbol parameter,
        INamedTypeSymbol? cancellationToken
    )
    {
        // The converted constant has the parameter's type, so `0` matches `long x = 0`
        if (value.ConstantValue.HasValue)
        {
            return Equals(value.ConstantValue.Value, parameter.ExplicitDefaultValue);
        }

        while (value is IConversionOperation { IsImplicit: true } conversion)
        {
            value = conversion.Operand;
        }

        // Conversions to a nullable type carry no constant; the operand does: `5` for `int? x = 5`
        if (value.ConstantValue.HasValue)
        {
            return Equals(value.ConstantValue.Value, parameter.ExplicitDefaultValue);
        }

        // A struct parameter declared `= default` reports a null default value
        if (parameter.ExplicitDefaultValue is not null || !parameter.Type.IsValueType)
        {
            return false;
        }

        return value switch
        {
            IDefaultValueOperation => true,
            IPropertyReferenceOperation { Property.Name: "None" } p =>
                SymbolEqualityComparer.Default.Equals(p.Property.ContainingType, cancellationToken),
            _ => false,
        };
    }
}

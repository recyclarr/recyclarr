using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Recyclarr.Analyzers;

/// <summary>
/// Reports lambdas that capture a local declared by a <c>using</c> declaration or statement. The
/// local is disposed when the outer scope ends, so a lambda that outlives that scope uses a
/// disposed object. The rule does not prove the lambda escapes; passing the value as a lambda
/// parameter makes the lifetime explicit. One case is exempt: a lambda stored in a local that is
/// used only as the subject of AwesomeAssertions <c>Should()</c> (<c>var act = () => ...;
/// act.Should().Throw()</c>), because the assertion runs the delegate before the scope ends.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CapturedUsingLocalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "REC002",
        "Lambda captures a using local",
        "Lambda captures '{0}', which is disposed at the end of the outer scope",
        "Reliability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.LocalReference);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var reference = (ILocalReferenceOperation)ctx.Operation;
        if (FindUsingDeclarator(reference.Local) is not { } declarator)
        {
            return;
        }

        for (var parent = reference.Parent; parent is not null; parent = parent.Parent)
        {
            if (
                parent is IAnonymousFunctionOperation lambda
                && !lambda.Syntax.Span.Contains(declarator.Span)
                && !IsOnlyAsserted(lambda)
            )
            {
                ctx.ReportDiagnostic(
                    Diagnostic.Create(Rule, reference.Syntax.GetLocation(), reference.Local.Name)
                );
                return;
            }
        }
    }

    private static bool IsOnlyAsserted(IAnonymousFunctionOperation lambda)
    {
        if (
            lambda.Parent
            is not IDelegateCreationOperation
            {
                Parent: IVariableInitializerOperation
                {
                    Parent: IVariableDeclaratorOperation declarator
                },
            }
        )
        {
            return false;
        }

        IOperation body = lambda;
        while (body.Parent is not null)
        {
            body = body.Parent;
        }

        return body.Descendants()
            .OfType<ILocalReferenceOperation>()
            .Where(r => SymbolEqualityComparer.Default.Equals(r.Local, declarator.Symbol))
            .All(IsAssertionSubject);
    }

    private static bool IsAssertionSubject(ILocalReferenceOperation reference)
    {
        IOperation operation = reference;
        while (operation.Parent is IConversionOperation conversion)
        {
            operation = conversion;
        }

        return operation.Parent
            is IArgumentOperation
            {
                Parent: IInvocationOperation
                {
                    TargetMethod: { Name: "Should", ContainingAssembly.Name: "AwesomeAssertions" },
                },
            };
    }

    private static VariableDeclaratorSyntax? FindUsingDeclarator(ILocalSymbol local)
    {
        return local
            .DeclaringSyntaxReferences.Select(r => r.GetSyntax())
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault(d =>
                d.Parent?.Parent switch
                {
                    LocalDeclarationStatementSyntax statement => statement.UsingKeyword != default,
                    UsingStatementSyntax => true,
                    _ => false,
                }
            );
    }
}

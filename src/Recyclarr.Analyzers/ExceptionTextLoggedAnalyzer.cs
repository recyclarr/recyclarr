using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Recyclarr.Analyzers;

/// <summary>
/// Reports <c>e.Message</c> passed to a Serilog message-template method, where <c>e</c> is a
/// catch variable. Logging only the text drops the stack trace and inner exceptions; Serilog
/// captures both when the exception goes to the method's exception parameter. Serilog marks its
/// template methods with <c>MessageTemplateFormatMethodAttribute</c>, which identifies them here.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class ExceptionTextLoggedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "REC003",
        "Exception text logged instead of the exception",
        "Pass '{0}' to the logger's exception parameter instead of logging '{0}.Message'",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var invocation = (IInvocationOperation)ctx.Operation;
        if (!IsTemplateLogMethod(invocation.TargetMethod))
        {
            return;
        }

        var messageReads = invocation
            .Arguments.SelectMany(a => a.Value.DescendantsAndSelf())
            .OfType<IPropertyReferenceOperation>()
            .Where(p => p.Property.Name == "Message");

        foreach (var read in messageReads)
        {
            if (
                read.Instance is ILocalReferenceOperation { Local: var local }
                && IsCatchLocal(local)
            )
            {
                ctx.ReportDiagnostic(
                    Diagnostic.Create(Rule, read.Syntax.GetLocation(), local.Name)
                );
            }
        }
    }

    private static bool IsTemplateLogMethod(IMethodSymbol method)
    {
        return method
            .GetAttributes()
            .Any(a => a.AttributeClass?.Name == "MessageTemplateFormatMethodAttribute");
    }

    private static bool IsCatchLocal(ILocalSymbol local)
    {
        return local.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is CatchDeclarationSyntax);
    }
}

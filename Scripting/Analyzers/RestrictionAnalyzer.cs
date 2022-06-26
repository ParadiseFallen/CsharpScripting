using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scripting.Analyzers;

public class RestrictionAnalyzer : DiagnosticAnalyzer
{
    private static DiagnosticDescriptor RestrictedMemberRule { get; } =
        new(
            nameof(RestrictedMemberRule),
            "Restricted type or member",
            "The type or member '{0}' is restricted",
            "Security",
            DiagnosticSeverity.Error,
            true);
    private static DiagnosticDescriptor RestrictedLanguageElementRule { get; } =
        new(
            nameof(RestrictedLanguageElementRule),
            "Restricted language element",
            "The language element '{0}' is restricted",
            "Security",
            DiagnosticSeverity.Error,
            true);
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(RestrictedLanguageElementRule, RestrictedMemberRule);


    public override void Initialize(AnalysisContext context)
    {
        //// ??
        //context.EnableConcurrentExecution();
        //// ??
        //context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSyntaxNodeAction(Analyze,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.QualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;

        if (IsQualifiedName(node.Parent!))
            return;


        var info = context.SemanticModel.GetSymbolInfo(node);
        if (info.Symbol == null)
            return;

        if (info.Symbol.Kind == SymbolKind.Method
            && info.Symbol.ContainingType.Name == "Environment"
            && info.Symbol.Name == "Exit")
        {
            context.ReportDiagnostic(Diagnostic.Create(RestrictedMemberRule,
               node.GetLocation(),
               info.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static bool IsQualifiedName(SyntaxNode arg) => arg.Kind() switch
    {
        SyntaxKind.AliasQualifiedName => true,
        SyntaxKind.QualifiedName => true,
        _ => false
    };
}

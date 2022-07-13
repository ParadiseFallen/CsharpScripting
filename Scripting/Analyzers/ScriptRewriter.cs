using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Scripting.Analyzers;
public class ScriptRewriter : CSharpSyntaxRewriter
{
    //https://johnkoerner.com/csharp/using-a-csharp-syntax-rewriter/

    // https://riptutorial.com/roslyn-scripting



    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var target = node.Expression as MemberAccessExpressionSyntax;

        if (target is null)
            return node;

        var invocation = string.Join(".", target
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>().Select(x => x.Identifier.ValueText));

        Console.WriteLine(invocation);

        if (invocation.Equals("Environment.Exit"))
            node.Parent!.ReplaceNode(node, ThrowNode);

        return node.Parent;
    }


    private static SyntaxNode ThrowNode { get; } = CompilationUnit()
        .WithMembers
        (
            SingletonList<MemberDeclarationSyntax>
            (
                GlobalStatement
                (
                    ThrowStatement
                    (
                        ObjectCreationExpression
                        (
                            IdentifierName("Exception")
                        )
                        .WithArgumentList
                        (
                            ArgumentList
                            (
                                SingletonSeparatedList<ArgumentSyntax>
                                (
                                    Argument
                                    (
                                        LiteralExpression
                                        (
                                            SyntaxKind.StringLiteralExpression,
                                            Literal("test")
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            )
        ).ChildNodes().First();
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.NetCore.Analyzers.Runtime
{
    /// <summary>
    /// CA1834: Prefer Memory/ReadOnlyMemory overloads for Stream ReadAsync/WriteAsync methods.
    /// </summary>
    public class PreferStreamAsyncMemoryOverloadsFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PreferStreamAsyncMemoryOverloads.RuleId);
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!context.Diagnostics.Any())
            {
                return;
            }

            Document doc = context.Document;
            SyntaxNode root = await doc.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root.FindNode(context.Span) is SyntaxNode expression)
            {
                string title = MicrosoftNetCoreAnalyzersResources.PreferStreamWriteAsyncMemoryOverloadsTitle;

                var action = new MyCodeAction(title,
                    async ct => await ConvertToPreferredWriteAsync(doc, expression, ct).ConfigureAwait(false),
                    equivalenceKey: title);

                context.RegisterCodeFix(action, context.Diagnostics);
            }
        }

        // TODO: If the using System.Threading is not added yet, add it
        // TODO: If the using System is not added yet, add it
        private async Task<Document> ConvertToPreferredWriteAsync(Document originalDoc, SyntaxNode nodeToFix, CancellationToken ct)
        {
            SemanticModel semanticModel = await originalDoc.GetSemanticModelAsync(ct).ConfigureAwait(false);
            Compilation compilation = semanticModel.Compilation;

            if (!compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemMemoryExtensions, out INamedTypeSymbol? memoryExtensionsType) ||
                memoryExtensionsType == null ||
                !compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemReadOnlyMemory1, out INamedTypeSymbol? readOnlyMemoryType) ||
                readOnlyMemoryType == null ||
                !compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingCancellationToken, out INamedTypeSymbol? cancellationTokenType) ||
                cancellationTokenType == null)
            {
                return originalDoc;
            }

            DocumentEditor editor = await DocumentEditor.CreateAsync(originalDoc, ct).ConfigureAwait(false);
            SyntaxGenerator generator = editor.Generator;

            if (semanticModel.GetOperation(nodeToFix, ct) is IInvocationOperation invocation &&
                invocation.Arguments.Length >= 3) // with or without cancellation token
            {
                if (invocation.Syntax is InvocationExpressionSyntax invocationExpressionSyntax &&
                    invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax memberAccessSyntax)
                {
                    SyntaxNode byteArrayNode = invocation.Arguments[0].Syntax.WithoutTrailingTrivia();

                    var exp = generator.InvocationExpression();

                    //SyntaxNode expression = generator.InvocationExpression(WellKnownTypeNames.SystemMemoryExtensions);
                    //var byteArrayAccessNode = generator.MemberAccessExpression(byteArrayNode, "AsMemory") as ExpressionSyntax;

                    //IMethodSymbol? ctDefaultConstructor = cancellationTokenType.Constructors.FirstOrDefault(x => x.GetParameters().Length == 0);

                    //generator.MemberAccessExpression(cancellationTokenType, ctDefaultConstructor);

                    //SyntaxNode ctNode = generator.InvocationExpression(memberAccessSyntax, byteArrayAccessNode);

                    //editor.ReplaceNode(nodeToFix, ctNode.WithTriviaFrom(nodeToFix));
                    //return editor.GetChangedDocument();
                }
                //var exp = generator.InvocationExpression(memberAccessSyntax, byteArrayAccessNode, ctNode);

                //IdentifierNameSyntax identifierSyntax = (IdentifierNameSyntax)memberAccessSyntax.Expression;
                //SyntaxToken identifierToken = identifierSyntax.Identifier;
                //string identifierName = identifierToken.ValueText;
                //IdentifierNameSyntax methodSyntax = (IdentifierNameSyntax)memberAccessSyntax.Name;
                //SyntaxToken methodToken = methodSyntax.Identifier;



                //char charValue = unitString[0];
                //SyntaxNode charLiteralExpressionNode = generator.LiteralExpression(charValue);
                //var charTypeNode = generator.TypeExpression(SpecialType.System_Char);
                //var charSyntaxNode = generator.LocalDeclarationStatement(charTypeNode, currentSymbol.Name, charLiteralExpressionNode, isConst: true);
                //var newRoot = generator.ReplaceNode(root, variableGroupDeclarationOperation.Syntax, charSyntaxNode);
                //return doc.WithSyntaxRoot(newRoot);


                //IArgumentOperation[] newArguments = new IArgumentOperation[] { };

                //IMethodSymbol? preferredMethod = type.GetMembers("WriteAsync").OfType<IMethodSymbol>()
                //    .FirstOrDefault(x => x.Parameters.Length == 2 &&
                //    x.Parameters[0].Type.Equals(readOnlyMemoryType) &&
                //    x.Parameters[1].Type.Equals(cancellationTokenType));

                //if (preferredMethod != null)
                //{
                //    SyntaxNode nodeReplacement = generator.invo;

                //    editor.ReplaceNode(nodeToFix, preferredMethod.WithTriviaFrom(nodeToFix));
                //    return editor.GetChangedDocument();
                //}
            }

            return originalDoc;
        }

        private sealed class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey) :
                base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}

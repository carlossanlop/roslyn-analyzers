// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.NetCore.Analyzers.Runtime
{
    /// <summary>
    /// CA1834: summary
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public sealed class PreferStreamAsyncMemoryOverloadsFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PreferStreamAsyncMemoryOverloads.RuleId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Document doc = context.Document;
            SyntaxNode root = await doc.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root.FindNode(context.Span) is SyntaxNode expression)
            {
                string title = MicrosoftNetCoreAnalyzersResources.PreferStreamAsyncMemoryOverloadsTitle;

                var action = new CodeActionInternal(title,
                    async ct => await ConvertToPreferredWriteAsync(doc, expression, ct).ConfigureAwait(false),
                    equivalenceKey: title);

                context.RegisterCodeFix(action, context.Diagnostics);
            }
        }

        private async Task<Document> ConvertToPreferredWriteAsync(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            SyntaxGenerator generator = editor.Generator;

            if (semanticModel.GetOperation(nodeToFix, cancellationToken) is IInvocationOperation invocation &&
                invocation.Arguments.Length > 0 &&
                invocation.GetReceiverType(semanticModel.Compilation, beforeConversion: true, cancellationToken) is INamedTypeSymbol type)
            {
                IMethodSymbol? replacement = type.GetMembers("WriteAsync").OfType<IMethodSymbol>().FirstOrDefault(x => x.Parameters.Length == 2);

                if (replacement == null)
                {
                    return document;
                }

                

                SyntaxNode nodeReplacement;

                editor.ReplaceNode(nodeToFix, nodeReplacement.WithTriviaFrom(nodeToFix));
                return editor.GetChangedDocument();
            }

            return document;
        }

        private sealed class CodeActionInternal : DocumentChangeAction
        {
            public CodeActionInternal(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey) :
                base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
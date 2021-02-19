// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.NetCore.Analyzers.Runtime;

namespace Microsoft.NetCore.CSharp.Analyzers.Runtime
{
    /// <summary>
    /// CA2017 - Call async methods when in an async method.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpCallAsyncMethodWhenSyncAnalyzer : CallAsyncMethodWhenSyncAnalyzer
    {
        protected override void RegisterCodeBlockStartAction(AnalysisContext context) =>
            context.RegisterCodeBlockStartAction<SyntaxKind>(RegisterCodeBlockStartActions);

        protected override void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (IsInTaskReturningMethodOrDelegate(context))
            {
                var invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;
                var memberAccessSyntax = invocationExpressionSyntax.Expression as MemberAccessExpressionSyntax;
                if (InspectMemberAccess(context, memberAccessSyntax, SyncBlockingMethods))
                {
                    // Don't return double-diagnostics.
                    return;
                }
            }
        }

        protected override void AnalyzePropertyGetter(SyntaxNodeAnalysisContext context)
        {
            var memberAccessSyntax = (MemberAccessExpressionSyntax)context.Node;
            if (IsInTaskReturningMethodOrDelegate(context))
            {
                _ = InspectMemberAccess(context, memberAccessSyntax, SyncBlockingProperties);
            }
        }

        private void RegisterCodeBlockStartActions(CodeBlockStartAnalysisContext<SyntaxKind> ctxt)
        {
            ctxt.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            ctxt.RegisterSyntaxNodeAction(AnalyzePropertyGetter, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static bool IsInTaskReturningMethodOrDelegate(SyntaxNodeAnalysisContext context)
        {
            // We want to scan invocations that occur inside Task and Task<T>-returning delegates or methods.
            // That is: methods that either are or could be made async.
            IMethodSymbol? methodSymbol = null;
            if (context.Node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() is AnonymousFunctionExpressionSyntax anonymousFunc)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(anonymousFunc, context.CancellationToken);
                methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            }
            else if (context.Node.FirstAncestorOrSelf<MethodDeclarationSyntax>() is MethodDeclarationSyntax methodDecl)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
            }

            return methodSymbol.HasAsyncCompatibleReturnType();
        }

        private static bool InspectMemberAccess(
            SyntaxNodeAnalysisContext context,
            [NotNullWhen(true)] MemberAccessExpressionSyntax? memberAccessSyntax,
            IEnumerable<SyncBlockingMethod> problematicMethods)
        {
            if (memberAccessSyntax is null)
            {
                return false;
            }

            if (context.SemanticModel.GetSymbolInfo(memberAccessSyntax, context.CancellationToken).Symbol is ISymbol memberSymbol)
            {
                foreach (var item in problematicMethods)
                {
                    if (item.Method.IsMatch(memberSymbol))
                    {
                        var location = memberAccessSyntax.Name.GetLocation();

                        var properties = ImmutableDictionary<string, string>.Empty
                            .Add(ExtensionMethodNamespaceKeyName, item.ExtensionMethodNamespace is object ? string.Join(".", item.ExtensionMethodNamespace) : string.Empty);
                        DiagnosticDescriptor descriptor;

                        var messageArgs = new List<object>(2)
                        {
                            item.Method.Name
                        };

                        if (item.AsyncAlternativeMethodName is object)
                        {
                            properties = properties.Add(AsyncMethodKeyName, item.AsyncAlternativeMethodName);
                            descriptor = CallAsyncMethodWhenSyncRule;
                            messageArgs.Add(item.AsyncAlternativeMethodName);
                        }
                        else
                        {
                            properties = properties.Add(AsyncMethodKeyName, string.Empty);
                            descriptor = CallAsyncMethodWhenSyncRuleNoAlternativeMethod;
                        }

                        var diagnostic = Diagnostic.Create(descriptor, location, properties, messageArgs.ToArray());
                        context.ReportDiagnostic(diagnostic);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

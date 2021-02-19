' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.NetCore.Analyzers.Runtime

Namespace Microsoft.NetCore.VisualBasic.Analyzers.Runtime
    ''' <summary>
    ''' RS0007: Avoid zero-length array allocations.
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicCallAsyncMethodWhenSyncAnalyzer
        Inherits CallAsyncMethodWhenSyncAnalyzer

        Protected Overrides Sub RegisterCodeBlockStartAction(context As AnalysisContext)
            context.RegisterCodeBlockStartAction(Of SyntaxKind)(AddressOf RegisterCodeBlockStartActions)
        End Sub

        Protected Overrides Sub AnalyzeInvocation(context As SyntaxNodeAnalysisContext)
            Throw New NotImplementedException()
        End Sub

        Protected Overrides Sub AnalyzePropertyGetter(context As SyntaxNodeAnalysisContext)
            Throw New NotImplementedException()
        End Sub

        Private Sub RegisterCodeBlockStartActions(ctxt As CodeBlockStartAnalysisContext(Of SyntaxKind))
            ctxt.RegisterSyntaxNodeAction(AddressOf AnalyzeInvocation, SyntaxKind.InvocationExpression)
            ctxt.RegisterSyntaxNodeAction(AddressOf AnalyzePropertyGetter, SyntaxKind.SimpleMemberAccessExpression)
        End Sub

    End Class
End Namespace
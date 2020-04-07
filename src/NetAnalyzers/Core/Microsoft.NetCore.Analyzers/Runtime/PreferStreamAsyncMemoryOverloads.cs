// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.NetCore.Analyzers.Runtime
{
    /// <summary>
    /// CA1834: summary
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class PreferStreamAsyncMemoryOverloads : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1834";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.PreferStreamAsyncMemoryOverloadsTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.PreferStreamAsyncMemoryOverloadsMessage), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.PreferStreamAsyncMemoryOverloadsDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));

        internal static DiagnosticDescriptor PreferStreamAsyncMemoryOverloadsRule = DiagnosticDescriptorHelper.Create(
                                                                                        RuleId,
                                                                                        s_localizableTitle,
                                                                                        s_localizableMessage,
                                                                                        DiagnosticCategory.Performance,
                                                                                        RuleLevel.IdeSuggestion,
                                                                                        s_localizableDescription,
                                                                                        isPortedFxCopRule: false,
                                                                                        isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(PreferStreamAsyncMemoryOverloadsRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterOperationAction(
                context => AnalyzeInvocationExpression(
                    (IInvocationOperation)context.Operation,
                    context.ReportDiagnostic),
                OperationKind.Invocation);
        }

        private static bool IsStreamWriteAsyncMethod(IMethodSymbol method)
        {
            return string.Equals(method.Name, "WriteAsync", StringComparison.Ordinal) &&
                method.ContainingType.ToString() == WellKnownTypeNames.SystemIOStream;
        }

        private static bool HasByteArrayArgument(IMethodSymbol method)
        {
            return method.Parameters.Length == 3 &&
                method.Parameters[0].Type.TypeKind == TypeKind.Array &&
                method.Parameters[1].Type.SpecialType == SpecialType.System_Int32 &&
                method.Parameters[2].Type.SpecialType == SpecialType.System_Int32;
        }

        private static void AnalyzeInvocationExpression(IInvocationOperation invocation
            , Action<Diagnostic> reportDiagnostic)
        {
            if (invocation.Arguments.Length > 0)
            {
                IMethodSymbol methodSymbol = invocation.TargetMethod;
                if (methodSymbol != null &&
                    IsStreamWriteAsyncMethod(methodSymbol) &&
                    HasByteArrayArgument(methodSymbol))
                {
                    reportDiagnostic(invocation.Syntax.CreateDiagnostic(PreferStreamAsyncMemoryOverloadsRule));
                }
            }
        }
    }
}





/////
//var memoryType = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemMemory);
//if (memoryType == null)
//{
//    return;
//}

//var readOnlyMemoryType = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemReadOnlyMemory);
//if (readOnlyMemoryType == null)
//{
//    return;
//}

//var streamType = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIOStream);
//if (streamType == null)
//{
//    return;
//}

//var byteType = context.Compilation.GetSpecialType(SpecialType.System_Byte);
//if (byteType == null)
//{
//    return;
//}

//Func<IMethodSymbol, bool> paramFilter = (m) => m.Parameters.Length == 4 && Equals(m.Parameters[0].Type, byteType) && m.Parameters[0].Kind == SymbolKind.ArrayType;

////var readAsyncMethod = streamType.GetMembers("ReadAsync").OfType<IMethodSymbol>().FirstOrDefault(paramFilter);
////if (readAsyncMethod == null)
////{
////    return;
////}

//var writeAsyncMethod = streamType.GetMembers("WriteAsync").OfType<IMethodSymbol>().FirstOrDefault(paramFilter);
//if (writeAsyncMethod == null)
//{
//    return;
//}

//context.RegisterOperationAction(operationContext =>
//{
//    var invocation = (IInvocationOperation)operationContext.Operation;
//    if (invocation.TargetMethod == null)
//    {
//        return;
//    }

//    var method = invocation.TargetMethod;
//    //if (method.Equals(readAsyncMethod))
//    //{

//    //    var suggestedReadAsyncMethod = streamType.GetMembers("ReadAsync").OfType<IMethodSymbol>().FirstOrDefault(m => m.Parameters.Length == 2 && Equals(m.Parameters[0].Type, memoryType));

//    //    if (suggestedReadAsyncMethod != null)
//    //    {
//    //        var diagnostic = Diagnostic.Create(
//    //            descriptor: PreferStreamAsyncMemoryOverloadsRule,
//    //            location: invocation.Syntax.GetLocation(),
//    //            operationContext.ContainingSymbol.Name,
//    //            method.Name,
//    //            suggestedReadAsyncMethod.Name);
//    //    }
//    //}
//    //else 
//    if (method.Equals(writeAsyncMethod))
//    {
//        var suggestedWriteAsyncMethod = streamType.GetMembers("ReadAsync").OfType<IMethodSymbol>().FirstOrDefault(m => m.Parameters.Length == 2 && Equals(m.Parameters[0].Type, readOnlyMemoryType));

//        if (suggestedWriteAsyncMethod != null)
//        {
//            var diagnostic = Diagnostic.Create(
//                descriptor: PreferStreamAsyncMemoryOverloadsRule,
//                location: invocation.Syntax.GetLocation(),
//                operationContext.ContainingSymbol.Name,
//                method.Name,
//                suggestedWriteAsyncMethod.Name);
//        }
//    }

//}, OperationKind.Invocation);
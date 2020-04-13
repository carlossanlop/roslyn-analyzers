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
    /// CA1834: TODO
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PreferStreamAsyncMemoryOverloads : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1834";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.PreferStreamAsyncMemoryOverloadsTitle),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.PreferStreamAsyncMemoryOverloadsMessage),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));

        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.PreferStreamAsyncMemoryOverloadsDescription),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));

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
            context.RegisterOperationAction(OnCompilationStart, OperationKind.Invocation);
        }

        private static void OnCompilationStart(OperationAnalysisContext context)
        {
            if (!context.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIOStream, out INamedTypeSymbol? streamSymbol) ||
                streamSymbol == null)
            {
                return;
            }

            if (context.Operation is IInvocationOperation invocation &&
                invocation.Arguments.Length > 0 &&
                invocation.TargetMethod is IMethodSymbol method &&
                IsStreamWriteAsyncMethod(method, streamSymbol) &&
                HasUndesiredArguments(method))
            {
                context.ReportDiagnostic(invocation.Syntax.CreateDiagnostic(PreferStreamAsyncMemoryOverloadsRule));
            }
        }

        private static bool IsStreamWriteAsyncMethod(IMethodSymbol method, INamedTypeSymbol streamSymbol)
        {
            return method.ContainingType.Equals(streamSymbol) &&
                string.Equals(method.Name, "WriteAsync", StringComparison.Ordinal);
        }

        private static bool HasUndesiredArguments(IMethodSymbol method)
        {
            return method.Parameters.Length >= 3 && // with or without cancellation token
                method.Parameters[0].Type.TypeKind == TypeKind.Array &&
                method.Parameters[0].Type is IArrayTypeSymbol arrayTypeSymbol &&
                arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_Byte &&
                method.Parameters[1].Type.SpecialType == SpecialType.System_Int32 &&
                method.Parameters[2].Type.SpecialType == SpecialType.System_Int32;
        }

    }
}
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
            context.RegisterOperationAction(
                context =>
                {
                    if (context.Operation is IInvocationOperation invocation &&
                        IsUndesiredStreamWriteAsyncMethod(invocation))
                    {
                        context.ReportDiagnostic(invocation.Syntax.CreateDiagnostic(PreferStreamAsyncMemoryOverloadsRule));
                    }
                },
                OperationKind.Invocation);
        }

        private static bool IsUndesiredStreamWriteAsyncMethod(IInvocationOperation invocation)
        {
            return invocation.Arguments.Length > 0 &&
                invocation.TargetMethod is IMethodSymbol method &&
                IsStreamWriteAsyncMethod(method) &&
                HasUndesiredArguments(method);
        }

        private static bool IsStreamWriteAsyncMethod(IMethodSymbol method)
        {
            return string.Equals(method.Name, "WriteAsync", StringComparison.Ordinal) &&
                method.ContainingType.ToString() == WellKnownTypeNames.SystemIOStream;
        }

        private static bool HasUndesiredArguments(IMethodSymbol method)
        {
            return method.Parameters.Length == 3 &&
                method.Parameters[0].Type.TypeKind == TypeKind.Array &&
                method.Parameters[1].Type.SpecialType == SpecialType.System_Int32 &&
                method.Parameters[2].Type.SpecialType == SpecialType.System_Int32;
        }

    }
}
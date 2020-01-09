﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeQuality.Analyzers.ApiDesignGuidelines
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AvoidOutParameters : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1021";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(MicrosoftCodeQualityAnalyzersResources.AvoidOutParametersTitle), MicrosoftCodeQualityAnalyzersResources.ResourceManager, typeof(MicrosoftCodeQualityAnalyzersResources));

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(MicrosoftCodeQualityAnalyzersResources.AvoidOutParametersMessage), MicrosoftCodeQualityAnalyzersResources.ResourceManager, typeof(MicrosoftCodeQualityAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(MicrosoftCodeQualityAnalyzersResources.AvoidOutParametersDescription), MicrosoftCodeQualityAnalyzersResources.ResourceManager, typeof(MicrosoftCodeQualityAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessage,
                                                                             DiagnosticCategory.Design,
                                                                             DiagnosticHelpers.DefaultDiagnosticSeverity,
                                                                             isEnabledByDefault: false,
                                                                             description: s_localizableDescription,
                                                                             helpLinkUri: "https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1021",
                                                                             customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            analysisContext.RegisterCompilationStartAction(csac =>
            {
                var outAttributeType = csac.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeInteropServicesOutAttribute);
                var analyzer = new MethodAnalyzer(outAttributeType);

                csac.RegisterSymbolAction(analyzer.Analyze, SymbolKind.Method);
            });
        }

        private class MethodAnalyzer
        {
            private readonly INamedTypeSymbol? outAttributeSymbol;

            public MethodAnalyzer(INamedTypeSymbol? outAttributeSymbol)
            {
                this.outAttributeSymbol = outAttributeSymbol;
            }

            public void Analyze(SymbolAnalysisContext analysisContext)
            {
                var methodSymbol = (IMethodSymbol)analysisContext.Symbol;

                if (!methodSymbol.MatchesConfiguredVisibility(analysisContext.Options, Rule, analysisContext.CancellationToken))
                {
                    return;
                }

                var numberOfOutParams = methodSymbol.Parameters.Count(IsOutParameter);

                if (numberOfOutParams >= 1 &&
                    !IsTryPatternMethod(methodSymbol, numberOfOutParams) &&
                    !IsDeconstructPattern(methodSymbol))
                {
                    analysisContext.ReportDiagnostic(methodSymbol.CreateDiagnostic(Rule));
                }
            }

            private bool IsOutParameter(IParameterSymbol parameterSymbol) =>
                parameterSymbol.RefKind == RefKind.Out ||
                // Handle VB.NET special case for out parameters
                (parameterSymbol.RefKind == RefKind.Ref && parameterSymbol.HasAttribute(outAttributeSymbol));

            private bool IsTryPatternMethod(IMethodSymbol methodSymbol, int numberOfOutParams) =>
                methodSymbol.Name.StartsWith("Try", StringComparison.Ordinal) &&
                methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean &&
                methodSymbol.Parameters.Length >= 1 &&
                IsOutParameter(methodSymbol.Parameters[methodSymbol.Parameters.Length - 1]) &&
                numberOfOutParams == 1;

            private bool IsDeconstructPattern(IMethodSymbol methodSymbol)
            {
                if (!methodSymbol.Name.Equals("Deconstruct", StringComparison.Ordinal) ||
                    !methodSymbol.ReturnsVoid ||
                    methodSymbol.Parameters.Length == 0)
                {
                    return false;
                }

                // The first parameter of a Deconstruct method can either be this XXX or out XXX
                if (!methodSymbol.IsExtensionMethod &&
                    !IsOutParameter(methodSymbol.Parameters[0]))
                {
                    return false;
                }

                return methodSymbol.Parameters.Skip(1).All(IsOutParameter);
            }
        }
    }
}
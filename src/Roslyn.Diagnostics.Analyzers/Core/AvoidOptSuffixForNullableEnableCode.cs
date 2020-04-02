﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AvoidOptSuffixForNullableEnableCode : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.AvoidOptSuffixForNullableEnableCodeRuleIdTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.AvoidOptSuffixForNullableEnableCodeRuleIdMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.AvoidOptSuffixForNullableEnableCodeRuleIdDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.AvoidOptSuffixForNullableEnableCodeRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(context =>
            {
                if (context.Symbol.Name.EndsWith("Opt", System.StringComparison.Ordinal) &&
                    context.Symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is { } node &&
                    context.Compilation.GetSemanticModel(node.SyntaxTree).GetNullableContext(node.SpanStart).AnnotationsEnabled())
                {
                    context.ReportDiagnostic(context.Symbol.CreateDiagnostic(Rule));
                }
            }, SymbolKind.Parameter, SymbolKind.Field);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.NetCore.Analyzers.Runtime
{
    /// <summary>
    /// CA1834: summary
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public sealed class PreferStreamAsyncMemoryOverloadsFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PreferStreamAsyncMemoryOverloads.RuleId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            return Task.CompletedTask;
        }
        public sealed override FixAllProvider GetFixAllProvider()
        {
            // For more information on Fix All Providers,
            // see https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
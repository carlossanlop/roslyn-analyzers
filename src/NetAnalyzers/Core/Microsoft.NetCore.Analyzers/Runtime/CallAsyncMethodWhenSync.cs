// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.NetCore.Analyzers.Runtime
{
    /// <summary>
    /// CA2017 - Call async methods when in an async method.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public abstract class CallAsyncMethodWhenSyncAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA2017";

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.CallAsyncMethodWhenSyncMessage),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));

        private static readonly LocalizableString s_localizableMessageNoAlternativeMethod = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.CallAsyncMethodWhenSyncMessageNoAlternativeMethod),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.CallAsyncMethodWhenSyncTitle),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));

        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.CallAsyncMethodWhenSyncDescription),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));

        internal static DiagnosticDescriptor CallAsyncMethodWhenSyncRule = DiagnosticDescriptorHelper.Create(
            RuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.Reliability,
            RuleLevel.IdeSuggestion,
            s_localizableDescription,
            isPortedFxCopRule: false,
            isDataflowRule: false);

        internal static DiagnosticDescriptor CallAsyncMethodWhenSyncRuleNoAlternativeMethod = DiagnosticDescriptorHelper.Create(
            RuleId,
            s_localizableTitle,
            s_localizableMessageNoAlternativeMethod,
            DiagnosticCategory.Reliability,
            RuleLevel.IdeSuggestion,
            s_localizableDescription,
            isPortedFxCopRule: false,
            isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(CallAsyncMethodWhenSyncRule, CallAsyncMethodWhenSyncRuleNoAlternativeMethod);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            RegisterCodeBlockStartAction(context);
        }

        protected abstract void RegisterCodeBlockStartAction(AnalysisContext context);

        protected abstract void AnalyzeInvocation(SyntaxNodeAnalysisContext context);

        protected abstract void AnalyzePropertyGetter(SyntaxNodeAnalysisContext context);

        protected const string AsyncMethodKeyName = "AsyncMethodName";

        protected const string ExtensionMethodNamespaceKeyName = "ExtensionMethodNamespace";

        internal static readonly IReadOnlyList<SyncBlockingMethod> SyncBlockingProperties = new[]
        {
            new SyncBlockingMethod(new QualifiedMember(new QualifiedType(Namespaces.SystemThreadingTasks, nameof(Task)), nameof(Task<int>.Result)), null),
            new SyncBlockingMethod(new QualifiedMember(new QualifiedType(Namespaces.SystemThreadingTasks, nameof(ValueTask)), nameof(ValueTask<int>.Result)), null),
        };

        internal static readonly IEnumerable<SyncBlockingMethod> SyncBlockingMethods = new[]
        {
            new SyncBlockingMethod(new QualifiedMember(new QualifiedType(Namespaces.SystemThreadingTasks, nameof(Task)), nameof(Task.Wait)), null),
            new SyncBlockingMethod(new QualifiedMember(new QualifiedType(Namespaces.SystemThreadingTasks, nameof(Task)), nameof(Task.WaitAll)), null),
            new SyncBlockingMethod(new QualifiedMember(new QualifiedType(Namespaces.SystemThreadingTasks, nameof(Task)), nameof(Task.WaitAny)), null),
            new SyncBlockingMethod(new QualifiedMember(new QualifiedType(Namespaces.SystemRuntimeCompilerServices, nameof(TaskAwaiter)), nameof(TaskAwaiter.GetResult)), null),
            new SyncBlockingMethod(new QualifiedMember(new QualifiedType(Namespaces.SystemRuntimeCompilerServices, nameof(ValueTaskAwaiter)), nameof(ValueTaskAwaiter.GetResult)), null),
        };
    }

    internal static class Namespaces
    {
        internal static readonly IReadOnlyList<string> SystemCollectionsGeneric = new[]
        {
            nameof(System),
            nameof(System.Collections),
            nameof(System.Collections.Generic),
        };

        internal static readonly IReadOnlyList<string> SystemRuntimeCompilerServices = new[]
        {
            nameof(System),
            nameof(System.Runtime),
            nameof(System.Runtime.CompilerServices),
        };

        internal static readonly IReadOnlyList<string> SystemThreadingTasks = new[]
        {
            nameof(System),
            nameof(System.Threading),
            nameof(System.Threading.Tasks),
        };
    }

    internal static class AsyncMethodBuilderAttribute
    {
        internal const string TypeName = nameof(System.Runtime.CompilerServices.AsyncMethodBuilderAttribute);

        internal static readonly IReadOnlyList<string> Namespace = Namespaces.SystemRuntimeCompilerServices;
    }

    internal readonly struct QualifiedMember : IEquatable<QualifiedMember>
    {
        public QualifiedMember(QualifiedType containingType, string methodName)
        {
            ContainingType = containingType;
            Name = methodName;
        }

        public QualifiedType ContainingType { get; }

        public string Name { get; }

        public bool IsMatch(ISymbol symbol) => symbol?.Name == Name && ContainingType.IsMatch(symbol.ContainingType);

        public override string ToString() => ContainingType.ToString() + "." + Name;

        public bool Equals(QualifiedMember other) => other.ContainingType == ContainingType && other.Name == Name;

        public override bool Equals(object obj) => obj is QualifiedMember other && Equals(other);

        public static bool operator ==(QualifiedMember left, QualifiedMember right) => left.Equals(right);

        public static bool operator !=(QualifiedMember left, QualifiedMember right) => !(left == right);

        public override int GetHashCode() => base.GetHashCode();
    }

    [DebuggerDisplay("{" + nameof(Method) + "} -> {" + nameof(AsyncAlternativeMethodName) + "}")]
    internal readonly struct SyncBlockingMethod : IEquatable<SyncBlockingMethod>
    {
        public SyncBlockingMethod(QualifiedMember method, string? asyncAlternativeMethodName = null, IReadOnlyList<string>? extensionMethodNamespace = null)
        {
            Method = method;
            AsyncAlternativeMethodName = asyncAlternativeMethodName;
            ExtensionMethodNamespace = extensionMethodNamespace;
        }

        public QualifiedMember Method { get; }

        public string? AsyncAlternativeMethodName { get; }

        public IReadOnlyList<string>? ExtensionMethodNamespace { get; }

        public override bool Equals(object obj) => obj is SyncBlockingMethod other && Equals(other);

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(SyncBlockingMethod left, SyncBlockingMethod right) => left.Equals(right);

        public static bool operator !=(SyncBlockingMethod left, SyncBlockingMethod right) => !(left == right);

        public bool Equals(SyncBlockingMethod other) =>
            other.Method == Method && other.AsyncAlternativeMethodName == AsyncAlternativeMethodName && ExtensionMethodNamespace == other.ExtensionMethodNamespace;
    }

    internal readonly struct QualifiedType : IEquatable<QualifiedType>
    {
        public QualifiedType(IReadOnlyList<string> containingTypeNamespace, string typeName)
        {
            Namespace = containingTypeNamespace;
            Name = typeName;
        }

        public IReadOnlyList<string> Namespace { get; }

        public string Name { get; }

        public bool IsMatch(ISymbol symbol) => symbol?.Name == Name && symbol.BelongsToNamespace(Namespace);

        public override string ToString() => string.Join(".", Namespace.Concat(new[] { Name }));

        public override bool Equals(object obj) => obj is QualifiedType other && Equals(other);

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(QualifiedType left, QualifiedType right) => left.Equals(right);

        public static bool operator !=(QualifiedType left, QualifiedType right) => !(left == right);

        public bool Equals(QualifiedType other) => other.Namespace == Namespace && other.Name == Name;
    }

    internal static class Extensions
    {
        internal static bool HasAsyncCompatibleReturnType([NotNullWhen(true)] this IMethodSymbol? methodSymbol) =>
            IsAsyncCompatibleReturnType(methodSymbol?.ReturnType);

        /// <summary>
        /// Determines whether a type could be used with the async modifier as a method return type.
        /// </summary>
        /// <param name="typeSymbol">The type returned from a method.</param>
        /// <returns><c>true</c> if the type can be returned from an async method.</returns>
        /// <remarks>
        /// This is not the same thing as being an *awaitable* type, which is a much lower bar. Any type can be made awaitable by offering a GetAwaiter method
        /// that follows the proper pattern. But being an async-compatible type in this sense is a type that can be returned from a method carrying the async keyword modifier,
        /// in that the type is either the special Task type, or offers an async method builder of its own.
        /// </remarks>
        internal static bool IsAsyncCompatibleReturnType([NotNullWhen(true)] this ITypeSymbol? typeSymbol)
        {
            if (typeSymbol is null)
            {
                return false;
            }

            // ValueTask and ValueTask<T> have the AsyncMethodBuilderAttribute.
            // TODO: Use nameof(IAsyncEnumerable) after upgrade to netstandard2.1
            return (typeSymbol.Name == nameof(Task) && typeSymbol.BelongsToNamespace(Namespaces.SystemThreadingTasks))
                || (typeSymbol.Name == "IAsyncEnumerable" && typeSymbol.BelongsToNamespace(Namespaces.SystemCollectionsGeneric))
                || typeSymbol.GetAttributes().Any(ad =>
                        ad.AttributeClass?.Name == AsyncMethodBuilderAttribute.TypeName &&
                        ad.AttributeClass.BelongsToNamespace(AsyncMethodBuilderAttribute.Namespace));
        }

        internal static bool BelongsToNamespace(this ISymbol symbol, IReadOnlyList<string> namespaces)
        {
            if (symbol is null || namespaces is null || namespaces.Count == 0)
            {
                return false;
            }

            INamespaceSymbol currentNamespace = symbol.ContainingNamespace;
            for (int i = namespaces.Count - 1; i >= 0; i--)
            {
                if (currentNamespace?.Name != namespaces[i])
                {
                    return false;
                }

                currentNamespace = currentNamespace.ContainingNamespace;
            }

            return currentNamespace?.IsGlobalNamespace ?? false;
        }
    }
}
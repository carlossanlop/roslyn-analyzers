// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.CSharp.Analyzers.Runtime.CSharpCallAsyncMethodWhenSyncAnalyzer,
    Microsoft.NetCore.CSharp.Analyzers.Runtime.CSharpCallAsyncMethodWhenSyncFixer>;

namespace Microsoft.NetCore.Analyzers.Runtime.UnitTests
{
    public class CallAsyncMethodWhenSyncTests
    {
        private static readonly DiagnosticDescriptor Rule = CallAsyncMethodWhenSyncAnalyzer.CallAsyncMethodWhenSyncRule;
        private static readonly DiagnosticDescriptor RuleNoAlternativeMethod = CallAsyncMethodWhenSyncAnalyzer.CallAsyncMethodWhenSyncRuleNoAlternativeMethod;

        [Fact]
        public async Task TaskWaitInTaskReturningMethodGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Task t = null;
        t.Wait();
        return Task.FromResult(1);
    }
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task T() {
        Task t = null;
        await t;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(6, 11, 6, 15).WithArguments("Wait"), fixedCode);
        }

        [Fact]
        public async Task TaskWaitInValueTaskReturningMethodGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    ValueTask T() {
        Task t = null;
        t.Wait();
        return default;
    }
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async ValueTask T() {
        Task t = null;
        await t;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(6, 11, 6, 15).WithArguments("Wait"), fixedCode);
        }

        [Fact]
        public async Task TaskWait_InIAsyncEnumerableAsyncMethod_ShouldReportWarning()
        {
            var originalCode = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
class Test {
    async IAsyncEnumerable<int> FooAsync()
    {
        Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        yield return 1;
    }
}
";
            var fixedCode = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
class Test {
    async IAsyncEnumerable<int> FooAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        yield return 1;
    }
}
";
            var test = new VerifyCS.Test
            {
                TestCode = originalCode,
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                FixedCode = fixedCode
            };
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(9, 45, 9, 49).WithArguments("Wait"));
            await test.RunAsync();
        }

        [Fact]
        public async Task TaskOfTResultInTaskReturningMethodGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task<int> T() {
        Task<int> t = null;
        int result = t.Result;
        return Task.FromResult(result);
    }
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task<int> T() {
        Task<int> t = null;
        int result = await t;
        return result;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(6, 24, 6, 30).WithArguments("Result"), fixedCode);
        }

        [Fact]
        public async Task TaskOfTResultInTaskReturningMethodGeneratesWarning_FixPreservesCall()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Task<int> t = null;
        Assert.NotNull(t.Result);
        return Task.CompletedTask;
    }
}
static class Assert {
    internal static void NotNull(object value) => throw null;
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task T() {
        Task<int> t = null;
        Assert.NotNull(await t);
    }
}
static class Assert {
    internal static void NotNull(object value) => throw null;
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(6, 26, 6, 32).WithArguments("Result"), fixedCode);
        }

        [Fact]
        public async Task TaskOfTResultInTaskReturningMethodGeneratesWarning_FixRewritesCorrectExpression()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    async Task T() {
        await Task.Run(() => Console.Error).Result.WriteLineAsync();
    }
}
";

            var fixedCode = @"
using System;
using System.Threading.Tasks;
class Test {
    async Task T() {
        await (await Task.Run(() => Console.Error)).WriteLineAsync();
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(6, 45, 6, 51).WithArguments("Result"), fixedCode);
        }

        [Fact]
        public async Task TaskOfTResultInTaskReturningAnonymousMethodWithinSyncMethod_GeneratesWarning()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void T() {
        Func<Task<int>> f = delegate {
            Task<int> t = null;
            int result = t.Result;
            return Task.FromResult(result);
        };
    }
}
";

            var fixedCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void T() {
        Func<Task<int>> f = async delegate {
            Task<int> t = null;
            int result = await t;
            return result;
        };
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(8, 28, 8, 34).WithArguments("Result"), fixedCode);
        }

        [Fact]
        public async Task TaskOfTResultInTaskReturningSimpleLambdaWithinSyncMethod_GeneratesWarning()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void T() {
        Func<int, Task<int>> f = a => {
            Task<int> t = null;
            int result = t.Result;
            return Task.FromResult(result);
        };
    }
}
";

            var fixedCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void T() {
        Func<int, Task<int>> f = async a => {
            Task<int> t = null;
            int result = await t;
            return result;
        };
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(8, 28, 8, 34).WithArguments("Result"), fixedCode);
        }

        [Fact]
        public async Task TaskOfTResultInTaskReturningSimpleLambdaExpressionWithinSyncMethod_GeneratesWarning()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void T() {
        Task<int> b = null;
        Func<int, Task<int>> f = a => Task.FromResult(b.Result);
    }
}
";

            var fixedCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void T() {
        Task<int> b = null;
        Func<int, Task<int>> f = async a => await b;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(7, 57, 7, 63).WithArguments("Result"), fixedCode);
        }

        [Fact]
        public async Task TaskOfTResultInTaskReturningParentheticalLambdaWithinSyncMethod_GeneratesWarning()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void T() {
        Func<Task<int>> f = () => {
            Task<int> t = null;
            int result = t.Result;
            return Task.FromResult(result);
        };
    }
}
";

            var fixedCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void T() {
        Func<Task<int>> f = async () => {
            Task<int> t = null;
            int result = await t;
            return result;
        };
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(8, 28, 8, 34).WithArguments("Result"), fixedCode);
        }

        [Fact]
        public async Task TaskOfTResultInTaskReturningMethodAnonymousDelegate_GeneratesNoWarning()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    Task<int> T() {
        Task<int> task = null;
        task.ContinueWith(t => { Console.WriteLine(t.Result); });
        return Task.FromResult(1);
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(originalCode);
        }

        [Fact]
        public async Task TaskGetAwaiterGetResultInTaskReturningMethodGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Task t = null;
        t.GetAwaiter().GetResult();
        return Task.FromResult(1);
    }
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task T() {
        Task t = null;
        await t;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(6, 24, 6, 33).WithArguments("GetResult"), fixedCode);
        }

        [Fact]
        public async Task SyncInvocationWhereAsyncOptionExistsInSameTypeGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Foo(10, 15);
        return Task.FromResult(1);
    }
    internal static void Foo(int x, int y) { }
    internal static Task FooAsync(int x, int y) => null;
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task T() {
        await FooAsync(10, 15);
    }
    internal static void Foo(int x, int y) { }
    internal static Task FooAsync(int x, int y) => null;
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(Rule).WithSpan(6, 9, 6, 12).WithArguments("Foo", "FooAsync"), fixedCode);
        }

        [Fact]
        public async Task SyncInvocationWhereAsyncOptionIsObsolete_GeneratesNoWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Foo(10, 15);
        return Task.FromResult(1);
    }
    internal static void Foo(int x, int y) { }
    [System.Obsolete]
    internal static Task FooAsync(int x, int y) => null;
}
";

            await VerifyCS.VerifyAnalyzerAsync(originalCode);
        }

        [Fact]
        public async Task SyncInvocationWhereAsyncOptionIsPartlyObsolete_GeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Foo(10, 15.0);
        return Task.FromResult(1);
    }
    internal static void Foo(int x, int y) { }
    internal static void Foo(int x, double y) { }
    [System.Obsolete]
    internal static Task FooAsync(int x, int y) => null;
    internal static Task FooAsync(int x, double y) => null;
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task T() {
        await FooAsync(10, 15.0);
    }
    internal static void Foo(int x, int y) { }
    internal static void Foo(int x, double y) { }
    [System.Obsolete]
    internal static Task FooAsync(int x, int y) => null;
    internal static Task FooAsync(int x, double y) => null;
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(Rule).WithSpan(6, 9, 6, 12).WithArguments("Foo", "FooAsync"), fixedCode);
        }

        [Fact]
        public async Task SyncInvocationWhereAsyncOptionExistsInSubExpressionGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        int r = Foo().CompareTo(1);
        return Task.FromResult(1);
    }
    internal static int Foo() => 5;
    internal static Task<int> FooAsync() => null;
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task T() {
        int r = (await FooAsync()).CompareTo(1);
    }
    internal static int Foo() => 5;
    internal static Task<int> FooAsync() => null;
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(Rule).WithSpan(6, 17, 6, 20).WithArguments("Foo", "FooAsync"), fixedCode);
        }

        [Fact]
        public async Task SyncInvocationWhereAsyncOptionExistsInOtherTypeGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Util.Foo();
        return Task.FromResult(1);
    }
}
class Util {
    internal static void Foo() { }
    internal static Task FooAsync() => null;
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task T() {
        await Util.FooAsync();
    }
}
class Util {
    internal static void Foo() { }
    internal static Task FooAsync() => null;
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(Rule).WithSpan(6, 14, 6, 17).WithArguments("Foo", "FooAsync"), fixedCode);
        }

        [Fact]
        public async Task SyncInvocationWhereAsyncOptionExistsAsPrivateInOtherTypeGeneratesNoWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Util.Foo();
        return Task.FromResult(1);
    }
}
class Util {
    internal static void Foo() { }
    private static Task FooAsync() => null;
}
";

            await VerifyCS.VerifyAnalyzerAsync(originalCode);
        }

        [Fact]
        public async Task SyncInvocationWhereAsyncOptionExistsInOtherBaseTypeGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Apple a = null;
        a.Foo();
        return Task.FromResult(1);
    }
}
class Fruit {
    internal Task FooAsync() => null;
}
class Apple : Fruit {
    internal void Foo() { }
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task T() {
        Apple a = null;
        await a.FooAsync();
    }
}
class Fruit {
    internal Task FooAsync() => null;
}
class Apple : Fruit {
    internal void Foo() { }
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(Rule).WithSpan(7, 11, 7, 14).WithArguments("Foo", "FooAsync"), fixedCode);
        }

        [Fact]
        public async Task SyncInvocationWhereAsyncOptionExistsInExtensionMethodGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task T() {
        Fruit f = null;
        f.Foo();
        return Task.FromResult(1);
    }
}
class Fruit {
    internal void Foo() { }
}
static class FruitUtils {
    internal static Task FooAsync(this Fruit f) => null;
}
";

            var fixedCode = @"
using System.Threading.Tasks;
class Test {
    async Task T() {
        Fruit f = null;
        await f.FooAsync();
    }
}
class Fruit {
    internal void Foo() { }
}
static class FruitUtils {
    internal static Task FooAsync(this Fruit f) => null;
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(Rule).WithSpan(7, 11, 7, 14).WithArguments("Foo", "FooAsync"), fixedCode);
        }

        [Fact]
        public async Task SyncInvocationUsingStaticGeneratesWarning()
        {
            var originalCode = @"
using System.Threading.Tasks;
using static FruitUtils;
class Test {
    Task T() {
        Foo();
        return Task.FromResult(1);
    }
}
static class FruitUtils {
    internal static void Foo() { }
    internal static Task FooAsync() => null;
}
";

            var fixedCode = @"
using System.Threading.Tasks;
using static FruitUtils;
class Test {
    async Task T() {
        await FooAsync();
    }
}
static class FruitUtils {
    internal static void Foo() { }
    internal static Task FooAsync() => null;
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(Rule).WithSpan(7, 9, 7, 12).WithArguments("Foo", "FooAsync"), fixedCode);
        }

        [Fact]
        public async Task SyncInvocationUsingStaticGeneratesNoWarningAcrossTypes()
        {
            var originalCode = @"
using System.Threading.Tasks;
using static FruitUtils;
using static PlateUtils;
class Test {
    Task T() {
        // Foo and FooAsync are totally different methods (on different types).
        // The use of Foo should therefore not produce a recommendation to use FooAsync,
        // despite their name similarities.
        Foo();
        return Task.FromResult(1);
    }
}
static class FruitUtils {
    internal static void Foo() { }
}
static class PlateUtils {
    internal static Task FooAsync() => null;
}
";

            await VerifyCS.VerifyAnalyzerAsync(originalCode);
        }

        [Fact]
        public async Task AwaitingAsyncMethodWithoutSuffixProducesNoWarningWhereSuffixVersionExists()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    Task Foo() => null;
    Task FooAsync() => null;
    async Task BarAsync() {
       await Foo();
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(originalCode);
        }

        /// <summary>
        /// Verifies that when method invocations and member access happens in properties
        /// (which can never be async), nothing bad happens.
        /// </summary>
        /// <remarks>
        /// This may like a trivially simple case. But guess why we had to add a test for it? (it failed).
        /// </remarks>
        [Fact]
        public async Task NoDiagnosticAndNoExceptionForProperties()
        {
            var originalCode = @"
using System.Threading.Tasks;
class Test {
    string Foo => string.Empty;
    string Bar => string.Join(""a"", string.Empty);
}
";

            await VerifyCS.VerifyAnalyzerAsync(originalCode);
        }

        [Fact]
        public async Task GenericMethodName()
        {
            var originalCode = @"
using System.Threading.Tasks;
using static FruitUtils;
class Test {
    Task T() {
        Foo<int>();
        return Task.FromResult(1);
    }
}
static class FruitUtils {
    internal static void Foo<T>() { }
    internal static Task FooAsync<T>() => null;
}
";

            var fixedCode = @"
using System.Threading.Tasks;
using static FruitUtils;
class Test {
    async Task T() {
        await FooAsync<int>();
    }
}
static class FruitUtils {
    internal static void Foo<T>() { }
    internal static Task FooAsync<T>() => null;
}
";

            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(Rule).WithSpan(7, 9, 7, 17).WithArguments("Foo<int>", "FooAsync"), fixedCode);
        }

        [Fact]
        public async Task AsyncAlternative_CodeFixRespectsTrivia()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void Foo() { }
    Task FooAsync() => Task.CompletedTask;
    async Task DoWorkAsync()
    {
        await Task.Yield();
        Console.WriteLine(""Foo"");
        // Some comment
        Foo(/*argcomment*/); // another comment
    }
}
";
            var fixedCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void Foo() { }
    Task FooAsync() => Task.CompletedTask;
    async Task DoWorkAsync()
    {
        await Task.Yield();
        Console.WriteLine(""Foo"");
        // Some comment
        await FooAsync(/*argcomment*/); // another comment
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(Rule).WithSpan(15, 9, 15, 12).WithArguments("Foo", "FooAsync"), fixedCode);
        }

        [Fact]
        public async Task AwaitRatherThanWait_CodeFixRespectsTrivia()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void Foo() { }
    Task FooAsync() => Task.CompletedTask;
    async Task DoWorkAsync()
    {
        await Task.Yield();
        Console.WriteLine(""Foo"");
        // Some comment
        FooAsync(/*argcomment*/).Wait(); // another comment
    }
}
";
            var fixedCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void Foo() { }
    Task FooAsync() => Task.CompletedTask;
    async Task DoWorkAsync()
    {
        await Task.Yield();
        Console.WriteLine(""Foo"");
        // Some comment
        await FooAsync(/*argcomment*/); // another comment
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(originalCode, VerifyCS.Diagnostic(RuleNoAlternativeMethod).WithSpan(12, 34, 12, 38).WithArguments("Wait"), fixedCode);
        }
        [Fact]
        public async Task DoNotSuggestAsyncAlternativeWhenItIsSelf()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    public async Task CallMainAsync()
    {
        // do stuff
        CallMain();
        // do stuff
    }
    public void CallMain()
    {
        // more stuff
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(originalCode);
        }

        [Fact]
        public async Task DoNotSuggestAsyncAlternativeWhenItReturnsVoid()
        {
            var originalCode = @"
using System;
using System.Threading.Tasks;
class Test {
    void LogInformation() { }
    void LogInformationAsync() { }
    Task MethodAsync()
    {
        LogInformation();
        return Task.CompletedTask;
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(originalCode);
        }
    }
}

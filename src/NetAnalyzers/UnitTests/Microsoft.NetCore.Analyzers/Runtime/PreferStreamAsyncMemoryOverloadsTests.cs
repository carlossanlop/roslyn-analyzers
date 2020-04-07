// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Runtime.PreferStreamAsyncMemoryOverloads,
    Microsoft.NetCore.Analyzers.Runtime.PreferStreamAsyncMemoryOverloadsFixer>;
//using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
//    Microsoft.NetCore.Analyzers.Runtime.NormalizeStringsToUppercaseAnalyzer,
//    Microsoft.NetCore.VisualBasic.Analyzers.Runtime.BasicNormalizeStringsToUppercaseFixer>;

namespace Microsoft.NetCore.Analyzers.Runtime.UnitTests
{
    public class PreferStreamAsyncMemoryOverloadsTests
    {
        #region No Diagnostic Tests

        [Fact]
        public async Task NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync("");
        }

        #endregion

        #region Diagnostic Tests

        [Fact]
        public async Task Diagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.IO;
class C
{
    async void M()
    {
        byte[] buffer = { 0xBA, 0x5E, 0xBA, 0x11, 0xF0, 0x07, 0xBA, 0x11 };
        using (FileStream fs = new FileStream(""path.txt"", FileMode.Create))
        {
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}
            ", GetCSharpResult(10, 19));
        }

        #endregion


        #region Helpers

        private static DiagnosticResult GetCSharpResult(int line, int column)
            => VerifyCS.Diagnostic()
                .WithLocation(line, column);

        //private static DiagnosticResult GetCSharResult(int line, int column, string containingMethod, string invokedMethod, string suggestedMethod)
        //    => VerifyCS.Diagnostic()
        //        .WithLocation(line, column)
        //        .WithArguments(containingMethod, invokedMethod, suggestedMethod);

        //private static DiagnosticResult GetBasicDefaultResultAt(int line, int column, string containingMethod, string invokedMethod, string suggestedMethod)
        //    => VerifyVB.Diagnostic()
        //        .WithLocation(line, column)
        //        .WithArguments(containingMethod, invokedMethod, suggestedMethod);

        #endregion
    }
}

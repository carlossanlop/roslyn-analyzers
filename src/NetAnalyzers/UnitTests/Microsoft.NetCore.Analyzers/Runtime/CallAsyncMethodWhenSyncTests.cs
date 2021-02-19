// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.CSharp.Analyzers.Runtime.CSharpCallAsyncMethodWhenSyncAnalyzer,
    Microsoft.NetCore.CSharp.Analyzers.Runtime.CSharpCallAsyncMethodWhenSyncFixer>;

using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.NetCore.VisualBasic.Analyzers.Runtime.BasicCallAsyncMethodWhenSyncAnalyzer,
    Microsoft.NetCore.VisualBasic.Analyzers.Runtime.BasicCallAsyncMethodWhenSyncFixer>;

namespace Microsoft.NetCore.Analyzers.Runtime.UnitTests
{
    public class CallAsyncMethodWhenSyncTests
    {
        [Fact]
        public Task CS_Test()
        {
            string originalCode = @"";
            string fixedCode = @"";
            return VerifyCS.VerifyCodeFixAsync(originalCode, fixedCode);
        }

        [Fact]
        public Task VB_Test()
        {
            string originalCode = @"";
            string fixedCode = @"";
            return VerifyVB.VerifyCodeFixAsync(originalCode, fixedCode);
        }
    }
}

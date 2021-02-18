' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.NetCore.Analyzers.Runtime

Namespace Microsoft.NetCore.VisualBasic.Analyzers.Runtime

    <ExportCodeFixProvider(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicCallAsyncMethodWhenSyncFixer

        Inherits CallAsyncMethodWhenSyncFixer
    End Class

End Namespace

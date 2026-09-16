' File: ReadSeams.vb
' Project: CodeMem.Bridging
' Description: Test-only in-process seam for the read tools, the RunSeams shape (Articles XII, XIII; CON4's straddle fact, research R58).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' A hook a test may pass to <c>BridgeTools</c>. Production passes Nothing for the whole object and the hook has no effect.
''' </summary>
Public Class ReadSeams

    ''' <summary>Invoked once the call's map is open and its scope resolved, before the reader runs (B01 (6)). Test-only.</summary>
    Public Property AfterScopeResolved As Action

End Class

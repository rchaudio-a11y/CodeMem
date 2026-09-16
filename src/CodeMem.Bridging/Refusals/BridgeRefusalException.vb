' File: BridgeRefusalException.vb
' Project: CodeMem.Bridging
' Description: Carries a refusal from the door that raised it to the tool method that puts it on the wire as text (FR-306: never thrown on the wire).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Internal control flow only: every tool method catches it and answers with a CallToolResult whose IsError is true.
''' </summary>
Public Class BridgeRefusalException
    Inherits Exception

    ''' <summary>The refusal.</summary>
    Public ReadOnly Property Refusal As BridgeRefusal

    ''' <summary>
    ''' Wraps a refusal.
    ''' </summary>
    ''' <param name="refusal">The refusal.</param>
    Public Sub New(refusal As BridgeRefusal)
        MyBase.New(refusal.Text)
        Me.Refusal = refusal
    End Sub

End Class

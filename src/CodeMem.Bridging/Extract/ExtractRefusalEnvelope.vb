' File: ExtractRefusalEnvelope.vb
' Project: CodeMem.Bridging
' Description: A refusal inside an extract result: the kind and the text (contracts/tools.md §3.8).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Present when the request was refused; null when it ran.
''' </summary>
Public Class ExtractRefusalEnvelope

    ''' <summary>The refusal kind name.</summary>
    Public Property Kind As String

    ''' <summary>The refusal text.</summary>
    Public Property Text As String

End Class

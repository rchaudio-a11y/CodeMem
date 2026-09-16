' File: PartEnvelope.vb
' Project: CodeMem.Bridging
' Description: One declaring part of a symbol (058 §3.3).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Path, line, column and whether the part holds the symbol's name.
''' </summary>
Public Class PartEnvelope

    ''' <summary>The part's path.</summary>
    Public Property Path As String

    ''' <summary>The part's start line.</summary>
    Public Property Line As Integer

    ''' <summary>The part's start column.</summary>
    Public Property Column As Integer

    ''' <summary>True for the part containing the symbol's declaring location.</summary>
    Public Property ContainsDeclaringLocation As Boolean

End Class

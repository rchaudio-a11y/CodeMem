' File: PartRecord.vb
' Project: CodeMem.Core
' Description: One code_parts row as the bridge reads it (feature 004; 058 §3.3 parts).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' One declaring part of a symbol: its path and span.
''' </summary>
Public Class PartRecord

    ''' <summary>The declared symbol's row id.</summary>
    Public Property SymbolId As Long

    ''' <summary>The part's path, solution-relative.</summary>
    Public Property Path As String

    ''' <summary>Start offset.</summary>
    Public Property StartOffset As Integer

    ''' <summary>Length.</summary>
    Public Property Length As Integer

    ''' <summary>Start line.</summary>
    Public Property StartLine As Integer

    ''' <summary>Start column.</summary>
    Public Property StartColumn As Integer

End Class

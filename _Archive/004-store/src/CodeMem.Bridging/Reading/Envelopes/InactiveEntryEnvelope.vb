' File: InactiveEntryEnvelope.vb
' Project: CodeMem.Bridging
' Description: A registry row that is not active, listed without a verdict (contracts/tools.md §3.7).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Key, project and state.
''' </summary>
Public Class InactiveEntryEnvelope

    ''' <summary>The registry key.</summary>
    Public Property SolutionKey As String

    ''' <summary>The MemOS project id.</summary>
    Public Property ProjectId As Long

    ''' <summary>The row's state.</summary>
    Public Property State As String

End Class

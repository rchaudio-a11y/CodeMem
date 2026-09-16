' File: CompiledIntoEnvelope.vb
' Project: CodeMem.Bridging
' Description: One project a declaration is compiled into (contracts/tools.md §3.2; FR-340).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The declaring project and that project's own symbol id and doc-comment id.
''' </summary>
Public Class CompiledIntoEnvelope

    ''' <summary>The project row id.</summary>
    Public Property ProjectSymbolId As Long?

    ''' <summary>The project name.</summary>
    Public Property ProjectName As String

    ''' <summary>The symbol id under that project.</summary>
    Public Property SymbolId As Long

    ''' <summary>The doc-comment id under that project.</summary>
    Public Property DocCommentId As String

End Class

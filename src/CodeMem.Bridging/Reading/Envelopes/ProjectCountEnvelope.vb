' File: ProjectCountEnvelope.vb
' Project: CodeMem.Bridging
' Description: One project's orphan count (060 §3.1 byProject).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Every active project in scope is listed, zeros included.
''' </summary>
Public Class ProjectCountEnvelope

    ''' <summary>The solution id.</summary>
    Public Property SolutionId As Long

    ''' <summary>The project row id.</summary>
    Public Property ProjectSymbolId As Long

    ''' <summary>The project name.</summary>
    Public Property Name As String

    ''' <summary>Orphans declared by the project.</summary>
    Public Property Count As Integer

End Class

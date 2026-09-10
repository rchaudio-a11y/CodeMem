' File: SolutionRow.vb
' Project: CodeMem.Tests
' Description: A solutions row as read by the test queries.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' One <c>solutions</c> row for assertions.
''' </summary>
Public Class SolutionRow

    ''' <summary>Row id.</summary>
    Public Property Id As Long

    ''' <summary>Key.</summary>
    Public Property Key As String

    ''' <summary>Name.</summary>
    Public Property Name As String

    ''' <summary>Repository root label or Nothing.</summary>
    Public Property RepoRoot As String

    ''' <summary>Last seen path label.</summary>
    Public Property LastSeenPath As String

    ''' <summary>First completed run or Nothing.</summary>
    Public Property FirstRunId As Long?

End Class

' File: SolutionRecord.vb
' Project: CodeMem.Core
' Description: A solutions row as read from the map.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' One <c>solutions</c> row.
''' </summary>
Public Class SolutionRecord

    ''' <summary>Surrogate id: what fact rows carry as solution_id.</summary>
    Public Property Id As Long

    ''' <summary>The only identity column (FR-032).</summary>
    Public Property Key As String

    ''' <summary>Display name.</summary>
    Public Property Name As String

    ''' <summary>Repository working directory label, or Nothing.</summary>
    Public Property RepoRoot As String

    ''' <summary>Last solution path label.</summary>
    Public Property LastSeenPath As String

    ''' <summary>ISO-8601 UTC creation time.</summary>
    Public Property CreatedUtc As String

    ''' <summary>First completed run, or Nothing.</summary>
    Public Property FirstRunId As Long?

End Class

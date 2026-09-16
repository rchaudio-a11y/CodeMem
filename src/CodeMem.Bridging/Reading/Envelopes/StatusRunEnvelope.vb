' File: StatusRunEnvelope.vb
' Project: CodeMem.Bridging
' Description: The latest completed run of a map_status entry (contracts/tools.md §3.7).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Four facts from the map's run row.
''' </summary>
Public Class StatusRunEnvelope

    ''' <summary>The run id.</summary>
    Public Property RunId As Long

    ''' <summary>The recorded commit, or null.</summary>
    Public Property CommitSha As String

    ''' <summary>The recorded dirty flag, or null.</summary>
    Public Property IsDirty As Boolean?

    ''' <summary>Finish time.</summary>
    Public Property FinishedUtc As String

End Class

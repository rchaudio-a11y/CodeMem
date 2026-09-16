' File: HeadEnvelope.vb
' Project: CodeMem.Bridging
' Description: The repository's answers now for a map_status entry (contracts/tools.md §3.7).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Every fact null where it could not be read, with the note saying why.
''' </summary>
Public Class HeadEnvelope

    ''' <summary>HEAD's sha, or null.</summary>
    Public Property Sha As String

    ''' <summary>Whether the working tree is dirty, or null.</summary>
    Public Property TreeDirty As Boolean?

    ''' <summary>Commits from the recorded one to HEAD, or null when no count can be claimed.</summary>
    Public Property BehindBy As Integer?

    ''' <summary>The repository's own message when a fact could not be read, or null.</summary>
    Public Property Note As String

End Class

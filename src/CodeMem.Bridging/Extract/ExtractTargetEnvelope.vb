' File: ExtractTargetEnvelope.vb
' Project: CodeMem.Bridging
' Description: The target of an extract result: as given, and as resolved (contracts/tools.md §3.8).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' asGiven names the argument; the rest is null until resolution succeeds.
''' </summary>
Public Class ExtractTargetEnvelope

    ''' <summary>solutionKey=X, repoPath=P or stale.</summary>
    Public Property AsGiven As String

    ''' <summary>The registry key resolved to, or null.</summary>
    Public Property ResolvedKey As String

    ''' <summary>The map's last-seen path launched, or null.</summary>
    Public Property SolutionPath As String

    ''' <summary>The configured map.</summary>
    Public Property MapPath As String

End Class

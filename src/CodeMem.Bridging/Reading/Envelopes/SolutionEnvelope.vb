' File: SolutionEnvelope.vb
' Project: CodeMem.Bridging
' Description: One solution of the solutions result (056 §1.1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Labels and the latest run of any outcome, or null.
''' </summary>
Public Class SolutionEnvelope

    ''' <summary>The solution key.</summary>
    Public Property Key As String

    ''' <summary>The display name.</summary>
    Public Property Name As String

    ''' <summary>The repository root, or null.</summary>
    Public Property RepoRoot As String

    ''' <summary>The last solution path.</summary>
    Public Property LastSeenPath As String

    ''' <summary>The latest run, or null.</summary>
    Public Property LatestRun As LatestRunEnvelope

End Class

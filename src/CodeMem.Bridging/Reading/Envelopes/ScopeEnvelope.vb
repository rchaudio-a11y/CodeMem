' File: ScopeEnvelope.vb
' Project: CodeMem.Bridging
' Description: The scope every read tool reports: by solutionKey, the key, the one solution (005 data-model §3).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T014): projectId, the four registry counts, the dangling ids, hadNothingToSearch and reason are gone with the
' registry (FR-405); by is always solutionKey.

''' <summary>
''' By solutionKey only.
''' </summary>
Public Class ScopeEnvelope

    ''' <summary>Always solutionKey.</summary>
    Public Property By As String

    ''' <summary>The solution key.</summary>
    Public Property SolutionKey As String

    ''' <summary>The one solution in scope.</summary>
    Public Property Solutions As List(Of ScopeSolutionEnvelope)

End Class

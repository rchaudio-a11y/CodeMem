' File: ResolvedScope.vb
' Project: CodeMem.Bridging
' Description: What a resolved scope gives a reader: the envelope for the result and the solution ids to query (058 §3.1; data-model §3).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T014): Describe names the key only; the projectId branch is gone with the registry.

''' <summary>
''' Produced once per call by <see cref="ScopeResolver"/>.
''' </summary>
Public Class ResolvedScope

    ''' <summary>The scope as the result presents it.</summary>
    Public Property Envelope As ScopeEnvelope

    ''' <summary>The solution ids every query is bounded to; empty when there was nothing to search.</summary>
    Public Property SolutionIds As List(Of Long)

    ''' <summary>
    ''' The one-line description a refusal names: "solutionKey 'X'" (feature 005: the scope is always by key).
    ''' </summary>
    ''' <returns>The description.</returns>
    Public Function Describe() As String
        Return "solutionKey '" & Envelope.SolutionKey & "'"
    End Function

    ''' <summary>
    ''' Whether a solution id is in scope.
    ''' </summary>
    ''' <param name="solutionId">The id.</param>
    ''' <returns>True when in scope.</returns>
    Public Function Contains(solutionId As Long) As Boolean
        Return SolutionIds.Contains(solutionId)
    End Function

End Class

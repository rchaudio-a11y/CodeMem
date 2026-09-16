' File: ResolvedScope.vb
' Project: CodeMem.Bridging
' Description: What a resolved scope gives a reader: the envelope for the result and the solution ids to query (058 §3.1; data-model §3).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Produced once per call by <see cref="ScopeResolver"/>.
''' </summary>
Public Class ResolvedScope

    ''' <summary>The scope as the result presents it.</summary>
    Public Property Envelope As ScopeEnvelope

    ''' <summary>The solution ids every query is bounded to; empty when there was nothing to search.</summary>
    Public Property SolutionIds As List(Of Long)

    ''' <summary>
    ''' The one-line description a refusal names: "projectId N, resolving to: keys" or "solutionKey 'X'".
    ''' </summary>
    ''' <returns>The description.</returns>
    Public Function Describe() As String
        If Envelope.By = "solutionKey" Then Return "solutionKey '" & Envelope.SolutionKey & "'"
        Dim keys As List(Of String) = New List(Of String)()
        For Each solution As ScopeSolutionEnvelope In Envelope.Solutions
            keys.Add(solution.Key)
        Next
        Return "projectId " & Envelope.ProjectId.Value.ToString(Globalization.CultureInfo.InvariantCulture) & ", resolving to: " & If(keys.Count = 0, "no solution", String.Join(", ", keys))
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

' File: ExtractRequest.vb
' Project: CodeMem.Bridging
' Description: What an extract call asks for: the origin and exactly one of solutionKey, repoPath or stale (data-model §8; FR-325, FR-331).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' One request through the one door; the door validates cardinality (INC2).
''' </summary>
Public Class ExtractRequest

    ''' <summary>Which door the call came through.</summary>
    Public Property Origin As ExtractOrigin

    ''' <summary>A registry key, or Nothing.</summary>
    Public Property SolutionKey As String

    ''' <summary>A directory under a registered root, or Nothing.</summary>
    Public Property RepoPath As String

    ''' <summary>True to refresh every stale bound solution.</summary>
    Public Property Stale As Boolean

    ''' <summary>The log's target column when it is not the argument as given: "stale" for the per-solution runs of extract(stale) (INC3); Nothing otherwise.</summary>
    Public Property LogTarget As String

End Class

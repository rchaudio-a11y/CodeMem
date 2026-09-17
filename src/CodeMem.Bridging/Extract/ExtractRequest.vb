' File: ExtractRequest.vb
' Project: CodeMem.Bridging
' Description: What an extract call asks for: the origin and exactly one of solutionKey, repoPath or stale; solutionPath beside solutionKey (data-model §8; FR-325, FR-331; 005 FR-414, R67).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T027): SolutionPath added - the solution file named beside the key, the way a solution the map has never seen is added.

''' <summary>
''' One request through the one door; the door validates cardinality (INC2).
''' </summary>
Public Class ExtractRequest

    ''' <summary>Which door the call came through.</summary>
    Public Property Origin As ExtractOrigin

    ''' <summary>A map solution key, or Nothing.</summary>
    Public Property SolutionKey As String

    ''' <summary>The solution file (.sln or .slnx) to extract, only beside SolutionKey; Nothing to use the map's last-seen path (005 FR-414).</summary>
    Public Property SolutionPath As String

    ''' <summary>A directory under a mapped root, or Nothing.</summary>
    Public Property RepoPath As String

    ''' <summary>True to refresh every stale bound solution.</summary>
    Public Property Stale As Boolean

    ''' <summary>The log's target column when it is not the argument as given: "stale" for the per-solution runs of extract(stale) (INC3); Nothing otherwise.</summary>
    Public Property LogTarget As String

End Class

' File: MapStatusEntryEnvelope.vb
' Project: CodeMem.Bridging
' Description: One bound solution's status (contracts/tools.md §3.7; data-model §7).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The registry binding, the run, the root, the head and one verdict by name with its reason.
''' </summary>
Public Class MapStatusEntryEnvelope

    ''' <summary>The registry key.</summary>
    Public Property SolutionKey As String

    ''' <summary>The MemOS project id.</summary>
    Public Property ProjectId As Long

    ''' <summary>The bound map solution id.</summary>
    Public Property CodememSolutionId As Long

    ''' <summary>The latest completed run, or null.</summary>
    Public Property Run As StatusRunEnvelope

    ''' <summary>The map's repository root, or null.</summary>
    Public Property RepoRoot As String

    ''' <summary>The repository's answers.</summary>
    Public Property Head As HeadEnvelope

    ''' <summary>current, behind, dirty, no_git, map_missing_solution or diverged.</summary>
    Public Property Verdict As String

    ''' <summary>One sentence naming the facts.</summary>
    Public Property Reason As String

End Class

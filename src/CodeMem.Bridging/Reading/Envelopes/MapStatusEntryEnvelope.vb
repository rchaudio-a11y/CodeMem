' File: MapStatusEntryEnvelope.vb
' Project: CodeMem.Bridging
' Description: One map solution: its key and id, the run, the root, the head and one verdict by name with its reason (005 data-model §5).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T015): projectId and codememSolutionId are gone; solutionId is the map's own id.

''' <summary>
''' The solution, the run, the root, the head and one verdict by name with its reason.
''' </summary>
Public Class MapStatusEntryEnvelope

    ''' <summary>The solution key.</summary>
    Public Property SolutionKey As String

    ''' <summary>The map's solution id.</summary>
    Public Property SolutionId As Long

    ''' <summary>The latest completed run, or null.</summary>
    Public Property Run As StatusRunEnvelope

    ''' <summary>The map's repository root, or null when the run found none.</summary>
    Public Property RepoRoot As String

    ''' <summary>The repository's answers.</summary>
    Public Property Head As HeadEnvelope

    ''' <summary>current, behind, dirty, no_git or diverged.</summary>
    Public Property Verdict As String

    ''' <summary>One sentence naming the facts.</summary>
    Public Property Reason As String

End Class

' File: ExaminedRunEnvelope.vb
' Project: CodeMem.Bridging
' Description: One run rename_candidates examined (feature 006, T011; data-model §4; FR-503): its recorded counts, the candidates returned, and whether the count check ran.
' Author: RCH Automation LLC
' Created: 2026-09-29

''' <summary>
''' countNotCheckedReason is null when checked; otherwise "failed run" or "filtered by retiredSymbolId".
''' </summary>
Public Class ExaminedRunEnvelope

    ''' <summary>The run id.</summary>
    Public Property RunId As Long

    ''' <summary>completed or failed.</summary>
    Public Property Outcome As String

    ''' <summary>The run's finish time.</summary>
    Public Property FinishedUtc As String

    ''' <summary>The run's recorded symbols_retired.</summary>
    Public Property SymbolsRetired As Integer

    ''' <summary>The run's recorded rename_candidates.</summary>
    Public Property RenameCandidatesRecorded As Integer

    ''' <summary>The candidates of this run in the result.</summary>
    Public Property CandidatesReturned As Integer

    ''' <summary>Whether the returned candidates were checked against the recorded count.</summary>
    Public Property CountChecked As Boolean

    ''' <summary>Why the check did not run, or null.</summary>
    Public Property CountNotCheckedReason As String

End Class

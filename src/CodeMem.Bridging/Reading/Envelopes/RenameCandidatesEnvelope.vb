' File: RenameCandidatesEnvelope.vb
' Project: CodeMem.Bridging
' Description: The rename_candidates result (feature 006, T011; data-model §2): readAtUtc, scope, filters, symbol, runs, total, candidates, note.
' Author: RCH Automation LLC
' Created: 2026-09-29

''' <summary>
''' The whole answer. Nulls are written, never omitted: symbol is null without retiredSymbolId, note is null when total is above 0.
''' </summary>
Public Class RenameCandidatesEnvelope

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>The scope.</summary>
    Public Property Scope As ScopeEnvelope

    ''' <summary>The arguments as given.</summary>
    Public Property Filters As RenameCandidatesFiltersEnvelope

    ''' <summary>The symbol retiredSymbolId names, retired or active; null without that argument.</summary>
    Public Property Symbol As CandidateSideEnvelope

    ''' <summary>The runs examined, newest first (spec Q5).</summary>
    Public Property Runs As List(Of ExaminedRunEnvelope)

    ''' <summary>The number of candidates returned.</summary>
    Public Property Total As Integer

    ''' <summary>The candidates, run id descending, then retired id, rank and new id; never folded.</summary>
    Public Property Candidates As List(Of CandidateEnvelope)

    ''' <summary>The empty-answer sentence when total is 0; otherwise null.</summary>
    Public Property Note As String

End Class

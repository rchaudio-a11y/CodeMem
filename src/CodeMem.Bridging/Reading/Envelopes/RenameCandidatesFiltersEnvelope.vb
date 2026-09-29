' File: RenameCandidatesFiltersEnvelope.vb
' Project: CodeMem.Bridging
' Description: rename_candidates' filters as given (feature 006, T011; data-model §2; analyze I3: its own file, one type per file).
' Author: RCH Automation LLC
' Created: 2026-09-29

''' <summary>
''' runId and retiredSymbolId echoed; absent means null.
''' </summary>
Public Class RenameCandidatesFiltersEnvelope

    ''' <summary>The run asked for, or null.</summary>
    Public Property RunId As Long?

    ''' <summary>The retired symbol asked for, or null.</summary>
    Public Property RetiredSymbolId As Long?

End Class

' File: RunRow.vb
' Project: CodeMem.Tests
' Description: An extract_runs row as read by the test queries.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' One <c>extract_runs</c> row for assertions.
''' </summary>
Public Class RunRow

    ''' <summary>Row id.</summary>
    Public Property Id As Long

    ''' <summary>Solution id.</summary>
    Public Property SolutionId As Long

    ''' <summary>completed or failed.</summary>
    Public Property Outcome As String

    ''' <summary>Source digest.</summary>
    Public Property SourceDigest As String

    ''' <summary>Commit sha or Nothing.</summary>
    Public Property CommitSha As String

    ''' <summary>Dirty flag or Nothing.</summary>
    Public Property IsDirty As Boolean?

    ''' <summary>Build configuration.</summary>
    Public Property BuildConfiguration As String

    ''' <summary>Target framework.</summary>
    Public Property TargetFramework As String

    ''' <summary>Extractor version.</summary>
    Public Property ExtractorVersion As String

    ''' <summary>Schema version.</summary>
    Public Property SchemaVersion As Integer

    ''' <summary>Started timestamp.</summary>
    Public Property StartedUtc As String

    ''' <summary>Finished timestamp.</summary>
    Public Property FinishedUtc As String

    ''' <summary>symbols_observed.</summary>
    Public Property SymbolsObserved As Integer

    ''' <summary>symbols_matched.</summary>
    Public Property SymbolsMatched As Integer

    ''' <summary>symbols_reactivated.</summary>
    Public Property SymbolsReactivated As Integer

    ''' <summary>symbols_new.</summary>
    Public Property SymbolsNew As Integer

    ''' <summary>symbols_retired.</summary>
    Public Property SymbolsRetired As Integer

    ''' <summary>registry_active_before.</summary>
    Public Property RegistryActiveBefore As Integer

    ''' <summary>notes_orphaned.</summary>
    Public Property NotesOrphaned As Integer

    ''' <summary>rename_candidates.</summary>
    Public Property RenameCandidates As Integer

    ''' <summary>unaccounted_observed.</summary>
    Public Property UnaccountedObserved As Integer

    ''' <summary>unaccounted_registry.</summary>
    Public Property UnaccountedRegistry As Integer

End Class

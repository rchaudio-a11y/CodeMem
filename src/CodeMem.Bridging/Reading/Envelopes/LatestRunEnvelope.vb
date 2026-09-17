' File: LatestRunEnvelope.vb
' Project: CodeMem.Bridging
' Description: The latest run of a solution (056 §1.1 latestRun).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T036): Warnings - the count of the run's extract_run_warnings rows.

''' <summary>
''' The stamp, the outcome, the dirty state and the ten counts.
''' </summary>
Public Class LatestRunEnvelope

    ''' <summary>The run id.</summary>
    Public Property RunId As Long

    ''' <summary>completed or failed.</summary>
    Public Property Outcome As String

    ''' <summary>The source digest.</summary>
    Public Property SourceDigest As String

    ''' <summary>The commit sha, or null.</summary>
    Public Property CommitSha As String

    ''' <summary>unknown when there is no commit, else clean or dirty.</summary>
    Public Property DirtyState As String

    ''' <summary>The schema version written.</summary>
    Public Property SchemaVersion As Integer

    ''' <summary>The SDK version, or null on a version-1 row.</summary>
    Public Property SdkVersion As String

    ''' <summary>Start time.</summary>
    Public Property StartedUtc As String

    ''' <summary>Finish time.</summary>
    Public Property FinishedUtc As String

    ''' <summary>Symbols observed.</summary>
    Public Property SymbolsObserved As Integer

    ''' <summary>Symbols matched.</summary>
    Public Property SymbolsMatched As Integer

    ''' <summary>Symbols reactivated.</summary>
    Public Property SymbolsReactivated As Integer

    ''' <summary>Symbols new.</summary>
    Public Property SymbolsNew As Integer

    ''' <summary>Symbols retired.</summary>
    Public Property SymbolsRetired As Integer

    ''' <summary>Registry active before.</summary>
    Public Property RegistryActiveBefore As Integer

    ''' <summary>Notes orphaned.</summary>
    Public Property NotesOrphaned As Integer

    ''' <summary>Rename candidates.</summary>
    Public Property RenameCandidates As Integer

    ''' <summary>Unaccounted observed.</summary>
    Public Property UnaccountedObserved As Integer

    ''' <summary>Unaccounted registry.</summary>
    Public Property UnaccountedRegistry As Integer

    ''' <summary>The number of NuGet restore warnings recorded on the run (005 FR-430).</summary>
    Public Property Warnings As Integer

End Class

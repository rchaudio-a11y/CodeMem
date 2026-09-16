' File: ExtractRunEnvelope.vb
' Project: CodeMem.Bridging
' Description: The published run an extract read back: id and the ten counts (contracts/tools.md §3.8; Article VIII).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Present only when a completed run was published.
''' </summary>
Public Class ExtractRunEnvelope

    ''' <summary>The run id.</summary>
    Public Property RunId As Long

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

End Class

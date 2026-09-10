' File: RunCounts.vb
' Project: CodeMem.Core
' Description: The ten reconciliation counts of constitution Article VIII.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' The ten counts every <c>extract_runs</c> row records. The two residuals are filled only by <c>CountAuditor</c>.
''' </summary>
Public Class RunCounts

    ''' <summary>Symbols staged this run.</summary>
    Public Property SymbolsObserved As Integer

    ''' <summary>Symbols that matched an active registry row (A).</summary>
    Public Property SymbolsMatched As Integer

    ''' <summary>Symbols that reactivated a retired row (A').</summary>
    Public Property SymbolsReactivated As Integer

    ''' <summary>Symbols minted a new id.</summary>
    Public Property SymbolsNew As Integer

    ''' <summary>Active rows not observed this run.</summary>
    Public Property SymbolsRetired As Integer

    ''' <summary>Active rows of the solution before reconciliation.</summary>
    Public Property RegistryActiveBefore As Integer

    ''' <summary>Written as 0 until a notes table exists.</summary>
    Public Property NotesOrphaned As Integer

    ''' <summary>Rename candidate rows written.</summary>
    Public Property RenameCandidates As Integer

    ''' <summary>observed - (matched + reactivated + new).</summary>
    Public Property UnaccountedObserved As Integer

    ''' <summary>registry_active_before - (matched + retired).</summary>
    Public Property UnaccountedRegistry As Integer

    ''' <summary>
    ''' Returns a copy of these counts.
    ''' </summary>
    ''' <returns>A new instance with the same ten values.</returns>
    Public Function Copy() As RunCounts
        Return New RunCounts With {
            .SymbolsObserved = SymbolsObserved,
            .SymbolsMatched = SymbolsMatched,
            .SymbolsReactivated = SymbolsReactivated,
            .SymbolsNew = SymbolsNew,
            .SymbolsRetired = SymbolsRetired,
            .RegistryActiveBefore = RegistryActiveBefore,
            .NotesOrphaned = NotesOrphaned,
            .RenameCandidates = RenameCandidates,
            .UnaccountedObserved = UnaccountedObserved,
            .UnaccountedRegistry = UnaccountedRegistry}
    End Function

End Class

' File: ReconciliationResult.vb
' Project: CodeMem.Core
' Description: What the reconciler hands publication: refreshes, reactivations, inserts, retirements, candidates and counts.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' The outcome of <c>Reconciler.Reconcile</c>.
''' </summary>
Public Class ReconciliationResult

    ''' <summary>Symbols that matched an active row (A); their ids are in <see cref="ExistingIds"/>.</summary>
    Public Property Refreshes As List(Of ObservedSymbol) = New List(Of ObservedSymbol)()

    ''' <summary>Symbols that reactivate a retired row (A'); their ids are in <see cref="ExistingIds"/>.</summary>
    Public Property Reactivations As List(Of ObservedSymbol) = New List(Of ObservedSymbol)()

    ''' <summary>Symbols minted a new id.</summary>
    Public Property Inserts As List(Of ObservedSymbol) = New List(Of ObservedSymbol)()

    ''' <summary>Active rows not observed this run.</summary>
    Public Property Retirements As List(Of RegistryRow) = New List(Of RegistryRow)()

    ''' <summary>Rename candidates for new symbols (B)/(C).</summary>
    Public Property Candidates As List(Of RenameCandidate) = New List(Of RenameCandidate)()

    ''' <summary>The ten counts, residuals not yet computed.</summary>
    Public Property Counts As RunCounts = New RunCounts()

    ''' <summary>Doc-comment id to row id for every matched and reactivated symbol.</summary>
    Public Property ExistingIds As Dictionary(Of String, Long) = New Dictionary(Of String, Long)(StringComparer.Ordinal)

End Class

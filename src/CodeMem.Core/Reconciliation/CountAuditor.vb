' File: CountAuditor.vb
' Project: CodeMem.Core
' Description: Computes the two Article VIII residuals from the counts alone, independently of the reconciler (FR-025).
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' Its own file, its own logic: no reference to Reconciler. Both residuals are written even when zero.
''' </summary>
Public Module CountAuditor

    ''' <summary>
    ''' Returns a copy of the counts with unaccounted_observed and unaccounted_registry filled in.
    ''' </summary>
    ''' <param name="counts">The staged counts.</param>
    ''' <returns>The audited copy.</returns>
    Public Function Audit(counts As RunCounts) As RunCounts
        Dim audited As RunCounts = counts.Copy()
        audited.UnaccountedObserved = counts.SymbolsObserved - (counts.SymbolsMatched + counts.SymbolsReactivated + counts.SymbolsNew)
        audited.UnaccountedRegistry = counts.RegistryActiveBefore - (counts.SymbolsMatched + counts.SymbolsRetired)
        Return audited
    End Function

End Module

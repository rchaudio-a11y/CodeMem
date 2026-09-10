' File: CountAuditorTests.vb
' Project: CodeMem.Tests
' Description: Unit test (Article III permitted) of the residual formulas; carries its own fire demonstration.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 CountAuditor.Audit throws NotImplementedException - reported to the Architect.
' GREEN: 2026-09-09 after CountAuditor.Audit (T083).
' FIRE:  2026-09-09 changed '+ SymbolsNew' to '- SymbolsNew' in CountAuditor.Audit -> 4 of 4 red; reverted -> green.

Imports CodeMem.Core
Imports Xunit

''' <summary>
''' unaccounted_observed = observed - (matched + reactivated + new); unaccounted_registry = registry_active_before - (matched + retired).
''' </summary>
Public Class CountAuditorTests

    ''' <summary>
    ''' Consistent counts yield 0/0.
    ''' </summary>
    <Fact>
    Public Sub ConsistentCountsYieldZeroResiduals()
        Dim audited As RunCounts = CountAuditor.Audit(New RunCounts With {.SymbolsObserved = 10, .SymbolsMatched = 6, .SymbolsReactivated = 0, .SymbolsNew = 4, .SymbolsRetired = 2, .RegistryActiveBefore = 8})
        Assert.Equal(0, audited.UnaccountedObserved)
        Assert.Equal(0, audited.UnaccountedRegistry)
    End Sub

    ''' <summary>
    ''' One extra matched symbol shows up as -1 on both sides.
    ''' </summary>
    <Fact>
    Public Sub OneExtraMatchYieldsMinusOneOnBothSides()
        Dim audited As RunCounts = CountAuditor.Audit(New RunCounts With {.SymbolsObserved = 10, .SymbolsMatched = 7, .SymbolsReactivated = 0, .SymbolsNew = 4, .SymbolsRetired = 2, .RegistryActiveBefore = 8})
        Assert.Equal(-1, audited.UnaccountedObserved)
        Assert.Equal(-1, audited.UnaccountedRegistry)
    End Sub

    ''' <summary>
    ''' Reactivated rows count on the observed side only: they were not active before.
    ''' </summary>
    <Fact>
    Public Sub ReactivationsCountOnTheObservedSideOnly()
        Dim audited As RunCounts = CountAuditor.Audit(New RunCounts With {.SymbolsObserved = 10, .SymbolsMatched = 6, .SymbolsReactivated = 2, .SymbolsNew = 2, .SymbolsRetired = 2, .RegistryActiveBefore = 8})
        Assert.Equal(0, audited.UnaccountedObserved)
        Assert.Equal(0, audited.UnaccountedRegistry)
    End Sub

    ''' <summary>
    ''' Audit returns a copy and leaves the input untouched.
    ''' </summary>
    <Fact>
    Public Sub AuditReturnsACopy()
        Dim input As RunCounts = New RunCounts With {.SymbolsObserved = 3, .SymbolsMatched = 0, .SymbolsNew = 2}
        Dim audited As RunCounts = CountAuditor.Audit(input)
        Assert.NotSame(input, audited)
        Assert.Equal(0, input.UnaccountedObserved)
        Assert.Equal(1, audited.UnaccountedObserved)
    End Sub

End Class

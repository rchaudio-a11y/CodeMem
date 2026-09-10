' File: I15_ReactivationTests.vb
' Project: CodeMem.Tests
' Description: I15 - a rename followed by a revert reactivates the original row on its original id (Article VI (A'), SC-013).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 run 3 minted a new id (40) instead of reactivating the run-1 row (31) - reported to the Architect.
' GREEN: 2026-09-09 after (A') in Reconciler and Reactivate/ReadRetiredByDocIds (T105).
' FIRE:  2026-09-09 skipped (A') with 'ElseIf False AndAlso' -> red; reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Deactivate and reactivate are the two legs of one pair (Article XIV).
''' </summary>
<Collection("Fixture")>
Public Class I15_ReactivationTests

    ''' <summary>
    ''' Describe to Explain (run 2), Explain back to Describe (run 3): the run-1 row is active again, first_seen unchanged, one reactivation, no new row, no candidate.
    ''' </summary>
    <Fact>
    Public Sub RevertReactivatesTheOriginalRow()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim describe1 As SymbolRow = MapQueries.ReadSymbols(map.Path, solutionId).Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Widgets.BaseWidget.Describe")

                copy.Replace("Sample.Lib/Widgets.vb", "Sub Describe(", "Sub Explain(")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim rowsAfterRun2 As Integer = MapQueries.ReadSymbols(map.Path, solutionId).Count

                copy.Replace("Sample.Lib/Widgets.vb", "Sub Explain(", "Sub Describe(")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
                Assert.Equal(3, runs.Count)
                Dim symbols As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)

                Dim describe3 As SymbolRow = symbols.Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Widgets.BaseWidget.Describe" AndAlso s.IsActive)
                Assert.NotNull(describe3)
                Assert.Equal(describe1.Id, describe3.Id)
                Assert.Equal(runs(0).Id, describe3.FirstSeenRunId)
                Assert.Equal(runs(2).Id, describe3.LastSeenRunId)
                Dim explain As SymbolRow = symbols.Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Widgets.BaseWidget.Explain")
                Assert.False(explain.IsActive)

                Dim run3 As RunRow = runs(2)
                Assert.Equal(1, run3.SymbolsReactivated)
                Assert.Equal(0, run3.SymbolsNew)
                Assert.Equal(0, run3.RenameCandidates)
                Assert.Equal(0, run3.UnaccountedObserved)
                Assert.Equal(0, run3.UnaccountedRegistry)
                Assert.Equal(rowsAfterRun2, symbols.Count)
            End Using
        End Using
    End Sub

End Class

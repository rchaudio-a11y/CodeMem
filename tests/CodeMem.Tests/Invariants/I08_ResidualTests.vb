' File: I08_ResidualTests.vb
' Project: CodeMem.Tests
' Description: I8 - both residuals are zero on a clean run; a corrupted count is refused with exit 4 and a failed run row (SC-006).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 (first half) CountAuditor.Audit throws NotImplementedException so the run exits 1 - reported to the Architect.
' GREEN: 2026-09-09 (first half) after CountAuditor.Audit wired into step 10 (T084); (second half) after steps 9-10 with InsertFailed + Commit (T088).
' FIRE:  2026-09-09 inverted the residual branch to '= 0 AndAlso = 0' -> both halves red; reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The counts that reconcile (Article VIII).
''' </summary>
<Collection("Fixture")>
Public Class I08_ResidualTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' First half: after one clean run into a fresh map every symbol is new, nothing is matched, reactivated or retired, and both residuals are zero.
    ''' </summary>
    <Fact>
    Public Sub CleanRunHasZeroResiduals()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim run As RunRow = MapQueries.ReadRuns(map.Path)(0)
            Assert.True(run.SymbolsObserved > 0, "no symbols observed")
            Assert.Equal(0, run.UnaccountedObserved)
            Assert.Equal(0, run.UnaccountedRegistry)
            Assert.Equal(run.SymbolsObserved, run.SymbolsNew)
            Assert.Equal(0, run.SymbolsMatched)
            Assert.Equal(0, run.SymbolsReactivated)
            Assert.Equal(0, run.SymbolsRetired)
            Assert.Equal(0, run.RegistryActiveBefore)
            Assert.Equal(0, run.NotesOrphaned)
            Assert.Equal(0, run.RenameCandidates)
        End Using
    End Sub

    ''' <summary>
    ''' Second half: a count corrupted through the seam is refused with exit 4, one failed run row carrying the -1 residual, and no published row changed.
    ''' </summary>
    <Fact>
    Public Sub CorruptedCountIsRefusedWithExitFour()
        Using map As TempMap = New TempMap()
            Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim before As Dictionary(Of String, List(Of String)) = MapSnapshot.RowsForSolution(map.Path, solutionId)

            Dim seams As RunSeams = New RunSeams With {.CorruptStagedCounts = Sub(c As Core.RunCounts) c.SymbolsMatched += 1}
            Assert.Equal(ExitCode.ResidualMismatch, ExtractionRun.Execute(options, seams))

            Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
            Assert.Equal(2, runs.Count)
            Assert.Equal("failed", runs(1).Outcome)
            Assert.Equal(-1, runs(1).UnaccountedObserved)

            Dim after As Dictionary(Of String, List(Of String)) = MapSnapshot.RowsForSolution(map.Path, solutionId)
            For Each table As String In New String() {"code_symbols", "code_parts", "code_edges", "rename_candidates"}
                Assert.Equal(before(table), after(table))
            Next
        End Using
    End Sub

End Class

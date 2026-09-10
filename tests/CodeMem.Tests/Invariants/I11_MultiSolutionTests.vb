' File: I11_MultiSolutionTests.vb
' Project: CodeMem.Tests
' Description: I11 - a second solution in the same map leaves the first byte-identical; no id is shared between solutions (Article IX, SC-009).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   n/a - expected green from the solution_id scoping of every repository; the guard is trusted through its FIRE.
' GREEN: 2026-09-09 first run.
' FIRE:  2026-09-09 dropped 'WHERE solution_id = @solution_id' from CodePartsRepository.ReplaceForSolution -> red (code_parts of solution A changed); reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Write scope is exactly WHERE solution_id = the solution being extracted.
''' </summary>
<Collection("Fixture")>
Public Class I11_MultiSolutionTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' Sample.sln then Sample.Lib.vbproj into one map: A's rows unchanged, two solutions, disjoint ids, distinct namespace rows.
    ''' </summary>
    <Fact>
    Public Sub SecondSolutionLeavesTheFirstUntouched()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim solutionA As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim snapshotA As Dictionary(Of String, List(Of String)) = MapSnapshot.RowsForSolution(map.Path, solutionA)

            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.LibProjectPath, .DbPath = map.Path}, Nothing))
            Dim solutions As List(Of SolutionRow) = MapQueries.ReadSolutions(map.Path)
            Assert.Equal(2, solutions.Count)
            Assert.Equal("Sample", solutions(0).Key)
            Assert.Equal("Sample.Lib", solutions(1).Key)
            Dim solutionB As Long = solutions(1).Id

            Dim afterA As Dictionary(Of String, List(Of String)) = MapSnapshot.RowsForSolution(map.Path, solutionA)
            For Each table As String In MapQueries.SnapshotTables()
                Assert.True(snapshotA(table).SequenceEqual(afterA(table)), table & " of solution A changed")
            Next

            Dim symbolsA As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionA)
            Dim symbolsB As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionB)
            Assert.True(symbolsB.Count > 0, "solution B wrote no symbols")
            Assert.Empty(symbolsA.ConvertAll(Function(s As SymbolRow) s.Id).Intersect(symbolsB.ConvertAll(Function(s As SymbolRow) s.Id)))
            Assert.Empty(MapQueries.ReadParts(map.Path, solutionA).ConvertAll(Function(p As PartRow) p.Id).Intersect(MapQueries.ReadParts(map.Path, solutionB).ConvertAll(Function(p As PartRow) p.Id)))
            Assert.Empty(MapQueries.ReadEdges(map.Path, solutionA, Nothing).ConvertAll(Function(e As EdgeRow) e.Id).Intersect(MapQueries.ReadEdges(map.Path, solutionB, Nothing).ConvertAll(Function(e As EdgeRow) e.Id)))
            Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
            Assert.Equal(2, runs.Count)
            Assert.NotEqual(runs(0).SolutionId, runs(1).SolutionId)

            Dim widgetsA As SymbolRow = symbolsA.Find(Function(s As SymbolRow) s.DocCommentId = "N:Sample.Widgets")
            Dim widgetsB As SymbolRow = symbolsB.Find(Function(s As SymbolRow) s.DocCommentId = "N:Sample.Widgets")
            Assert.NotNull(widgetsA)
            Assert.NotNull(widgetsB)
            Assert.NotEqual(widgetsA.Id, widgetsB.Id)
        End Using
    End Sub

End Class

' File: US4_SolutionKeyTests.vb
' Project: CodeMem.Tests
' Description: US4 - --solution-key separates same-named solutions; repo_root and last_seen_path are labels refreshed per run, never the key (FR-032).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   n/a - --solution-key was parsed in Slice A (T048); recorded as the production-route proof.
' GREEN: 2026-09-09 first run.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The key is the only identity column of a solution.
''' </summary>
<Collection("Fixture")>
Public Class US4_SolutionKeyTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' The same .sln extracted as Sample and as Other yields two solutions; Other's run is all new; Sample's rows are untouched;
    ''' a run from a copy at another path refreshes Sample's labels while key and id stay.
    ''' </summary>
    <Fact>
    Public Sub KeyOverrideSeparatesSolutionsAndLabelsRefresh()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim sample As SolutionRow = MapQueries.ReadSolutions(map.Path)(0)
            Assert.Equal("Sample", sample.Key)
            Dim snapshot As Dictionary(Of String, List(Of String)) = MapSnapshot.RowsForSolution(map.Path, sample.Id)

            Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path) & " --solution-key Other")
            Assert.Equal(0, run.ExitCode)
            Dim solutions As List(Of SolutionRow) = MapQueries.ReadSolutions(map.Path)
            Assert.Equal(2, solutions.Count)
            Assert.Equal("Other", solutions(1).Key)
            Dim otherRun As RunRow = MapQueries.ReadRuns(map.Path).Find(Function(r As RunRow) r.SolutionId = solutions(1).Id)
            Assert.Equal(otherRun.SymbolsObserved, otherRun.SymbolsNew)
            Assert.Equal(0, otherRun.SymbolsMatched)
            Dim after As Dictionary(Of String, List(Of String)) = MapSnapshot.RowsForSolution(map.Path, sample.Id)
            For Each table As String In MapQueries.SnapshotTables()
                Assert.True(snapshot(table).SequenceEqual(after(table)), table & " of Sample changed")
            Next

            Using copy As FixtureCopy = New FixtureCopy()
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}, Nothing))
                Dim refreshed As SolutionRow = MapQueries.ReadSolutions(map.Path).Find(Function(s As SolutionRow) s.Key = "Sample")
                Assert.Equal(sample.Id, refreshed.Id)
                Assert.Equal(IO.Path.GetFullPath(copy.SolutionPath), refreshed.LastSeenPath)
                Assert.NotEqual(sample.LastSeenPath, refreshed.LastSeenPath)
                Assert.NotEqual(sample.RepoRoot, refreshed.RepoRoot)
                Assert.Equal(2, MapQueries.ReadSolutions(map.Path).Count)
            End Using
        End Using
    End Sub

End Class

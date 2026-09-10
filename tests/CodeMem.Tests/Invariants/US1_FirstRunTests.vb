' File: US1_FirstRunTests.vb
' Project: CodeMem.Tests
' Description: US1 scenario 1 - one run into a fresh map writes identity, solution and a completed stamped run; exit 0.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 NotImplementedException from ExtractionRun.Execute (reported to the Architect).
' GREEN: 2026-09-09 after Slice A (T036-T051): stamp, identity and solution row written, exit 0 in ~2 s.

Imports System.Diagnostics
Imports System.Text.RegularExpressions
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' First extraction of the fixture into a fresh map: stamp and identity.
''' </summary>
<Collection("Fixture")>
Public Class US1_FirstRunTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' Exit 0; one map_identity row; one solutions row keyed Sample; one completed extract_runs row with every stamp field; under 60 s (SC-010).
    ''' </summary>
    <Fact>
    Public Sub FirstRunWritesIdentitySolutionAndStamp()
        Using map As TempMap = New TempMap()
            Dim watch As Stopwatch = Stopwatch.StartNew()
            Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing)
            watch.Stop()
            Assert.Equal(ExitCode.Success, code)
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(60), "fixture extraction took " & watch.Elapsed.ToString())

            Assert.Equal(1L, MapQueries.CountRows(map.Path, "map_identity", 0))
            Assert.Equal(36, MapQueries.ReadMapGuid(map.Path).Length)
            Assert.Equal(1, MapQueries.ReadSchemaVersion(map.Path))

            Dim solutions As List(Of SolutionRow) = MapQueries.ReadSolutions(map.Path)
            Assert.Single(solutions)
            Assert.Equal("Sample", solutions(0).Key)

            Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
            Assert.Single(runs)
            Dim run As RunRow = runs(0)
            Assert.Equal("completed", run.Outcome)
            Assert.Equal(solutions(0).Id, run.SolutionId)
            Assert.Matches("^[0-9a-f]{64}$", run.SourceDigest)
            Assert.NotNull(run.CommitSha)
            Assert.Matches("^[0-9a-f]{40}$", run.CommitSha)
            Assert.True(run.IsDirty.HasValue, "is_dirty must be non-null when the fixture lives inside a repository")
            Assert.Equal("Debug", run.BuildConfiguration)
            Assert.False(String.IsNullOrEmpty(run.TargetFramework))
            Assert.Equal("0.1.0", run.ExtractorVersion)
            Assert.Equal(1, run.SchemaVersion)
            Assert.Matches("^\d{4}-\d{2}-\d{2}T", run.StartedUtc)
            Assert.Matches("^\d{4}-\d{2}-\d{2}T", run.FinishedUtc)
            Assert.True(run.SymbolsObserved >= 0 AndAlso run.SymbolsMatched >= 0 AndAlso run.SymbolsReactivated >= 0 AndAlso run.SymbolsNew >= 0 AndAlso run.SymbolsRetired >= 0 AndAlso run.RegistryActiveBefore >= 0 AndAlso run.NotesOrphaned >= 0 AndAlso run.RenameCandidates >= 0, "ten counts present")
            Assert.Equal(solutions(0).Id, run.SolutionId)
        End Using
    End Sub

End Class

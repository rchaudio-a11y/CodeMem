' File: F06_FreshMapTests.vb
' Project: CodeMem.Tests
' Description: F6 - a 0-byte file is fresh; a SQLite file with tables but no map_identity is refused untouched; an abort during first-run initialization leaves a fresh file (FR-105, FR-106, SC-103, SC-104).
' Author: RCH Automation LLC
' Created: 2026-09-10
'
' RED:   2026-09-10 not red: InspectSchema and the in-transaction creation landed with Slice A (T023/T024) before this file first ran (the Stage A
'        behaviour, "file exists -> not fresh -> no such table -> exit 1", was gone by then); green on first run, 3 of 3.
' GREEN: 2026-09-10 first run: 0-byte file -> version-2 map; foreign file -> exit 1 "not a map: <path>", hash unchanged, no journal;
'        DuringInitialize abort -> file still fresh, next run exit 0.
' FIRE:  2026-09-10 (T034) the Foreign case created the schema as if Fresh -> (b) red (exit 0 instead of 1: the foreign file gained tables);
'        reverted -> green.

Imports System.IO
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The three fresh-map cases of research R23, through ExtractionRun.Execute and the executable.
''' </summary>
<Collection("Fixture")>
Public Class F06_FreshMapTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' (a) a 0-byte file at --db is treated as fresh: exit 0, a complete version-2 map.
    ''' </summary>
    <Fact>
    Public Sub ZeroByteFileIsFresh()
        Using map As TempMap = New TempMap()
            File.WriteAllBytes(map.Path, New Byte() {})
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Assert.True(MapQueries.CountUserTables(map.Path) > 0, "no tables were created")
            Assert.Equal(2, MapQueries.ReadSchemaVersion(map.Path))
            Assert.Single(MapQueries.ReadRuns(map.Path))
        End Using
    End Sub

    ''' <summary>
    ''' (b) a SQLite file with a table but no map_identity is not a map: exit 1, exactly one stderr line naming the path, empty stdout,
    ''' file bytes unchanged.
    ''' </summary>
    <Fact>
    Public Sub ForeignDatabaseIsRefusedUntouched()
        Using map As TempMap = New TempMap()
            MapQueries.CreateForeignDatabase(map.Path)
            Dim before As String = MapSnapshot.FileBytesHash(map.Path)
            Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path))
            Assert.Equal(1, run.ExitCode)
            Assert.Equal("", run.StandardOutput)
            Dim lines As String() = run.StandardError.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Assert.Single(lines)
            Assert.Contains("not a map: ", lines(0))
            Assert.Contains(map.Path, lines(0))
            Assert.Equal(before, MapSnapshot.FileBytesHash(map.Path))
            Assert.False(File.Exists(map.Path & "-journal"), "a journal was left beside the refused file")
        End Using
    End Sub

    ''' <summary>
    ''' (c) an abort during first-run initialization (after the schema and identity row, before the migration) leaves a file that is
    ''' still fresh (no user table, or absent or empty); the next run on the same path succeeds.
    ''' </summary>
    <Fact>
    Public Sub AbortDuringInitializeLeavesAFreshFileAndTheNextRunSucceeds()
        Using map As TempMap = New TempMap()
            Dim nonce As String = Guid.NewGuid().ToString("N")
            Dim environment As Dictionary(Of String, String) = New Dictionary(Of String, String) From {{"CODEMEM_TEST_ABORT_AT", "DuringInitialize:" & nonce}, {"CODEMEM_TEST_NONCE", nonce}}
            Dim aborted As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path), environment)
            Assert.NotEqual(0, aborted.ExitCode)
            Assert.Contains("CODEMEM_TEST_ABORT_AT=DuringInitialize", aborted.StandardError)

            Dim fresh As Boolean = Not File.Exists(map.Path) OrElse New FileInfo(map.Path).Length = 0 OrElse MapQueries.CountUserTables(map.Path) = 0
            Assert.True(fresh, "the file left behind is not fresh")

            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Assert.Equal(2, MapQueries.ReadSchemaVersion(map.Path))
            Assert.Single(MapQueries.ReadRuns(map.Path))
        End Using
    End Sub

End Class

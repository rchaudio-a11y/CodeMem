' File: S02_SchemaUpgradeTests.vb
' Project: CodeMem.Tests
' Description: Schema version 2 - a Stage A (version-1) map upgrades in place under the write lock; one shape with a fresh map; an abort during the upgrade rolls back (FR-111..FR-114, SC-102, SC-104, SC-106).
' Author: RCH Automation LLC
' Created: 2026-09-10
'
' RED:   2026-09-10 (1) red: the version-1 copy ran (Stage A accepted it because Current was 1) but schema_version still read 1 and the run had no
'        sdk_version; (2) red: neither map had a tr_ trigger; (3) was already green (Stage A refused any version other than 1) and stays as the FR-114 guard.
' GREEN: 2026-09-10 after T018-T025 (Current = 2, CreateVersion1 + UpgradeToVersion2 on both paths, InspectSchema, sdk_version stamp): 3 of 3;
'        the upgraded fixture and a fresh map have identical sqlite_master (type, name, tbl_name, sql), including both triggers.
' FIRE:  2026-09-10 (2) dropped tr_extract_runs_sdk_version_update on the Version1 path only (fresh path kept it) -> red (collections differ: the
'        update trigger missing from the upgraded map); reverted -> green.
' GREEN: 2026-09-10 (4) abort DuringUpgrade (T027): green on first run, as expected from T024 - the whole upgrade sits in the BEGIN IMMEDIATE.
' FIRE:  2026-09-10 (4) as specified in T027 - AbortIf(DuringUpgrade) moved after SetSchemaVersion(2) - did NOT go red: both statements are inside
'        the one transaction the FailFast discards, so the map reads 1 either way (that is FR-113 holding, not a gap in the test). Substitute
'        fire: committed the upgrade on its own (db.Commit() : db.BeginImmediate()) before the abort -> red (expected 1, actual 2); reverted -> green.
'
' 2026-09-13 (fixpack 003, rule 2; a Red named in plan 003 §Test design before it ran): (1) red - Assert.Equal(before("code_edges"),
' after("code_edges")) expected 137, actual 144. The Stage A extractor wrote 137 edges for the fixture; this executable also writes its seven
' bare-name occurrences. code_edges is an observation table replaced wholesale per run (Article VI), so its count follows the extractor, not
' the map's history; "every table keeps its rows" was and remains the fact for the identity and run tables. Amended in place: the upgraded map
' must hold exactly the edges a fresh map of the same fixture gets from the same executable. code_symbols, code_parts and rename_candidates
' equality stand (the fixture has no out-of-scope declaration). Re-run -> green.
'
' 2026-09-17 (feature 005, T008; Reds named in plan 005 §Test design): (1) and (4) assert 2 and (3) hand-bumps to 3 to provoke the newer-version
' refusal; all three go red at T009 when Current becomes 3 - (1), (4): 2 -> 3; (3): the bump -> 4 - and are amended in that task after the Red
' is observed.
' RED:   2026-09-17 (T009) (1) and (4) observed as named (expected 2, actual 3). (3) stayed green for the wrong reason: a version-1 map bumped to
'        3 now reads as Current, proceeds, and fails on the missing sdk_version column (exit 1) - the assertions held by accident, not by the
'        FR-114 refusal. Amended: (1), (4) to 3; (3) bumps to 4 so the refusal, not a column, is what exits 1. Re-run -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Runs the new extractor against copies of Fixtures/Maps/sample-v1.sqlite (research R30) and against a fresh map.
''' </summary>
<Collection("Fixture")>
Public Class S02_SchemaUpgradeTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' (1) a version-1 map upgrades: exit 0, schema_version 2, every table keeps its rows plus this run's own, old runs read sdk_version NULL,
    ''' the new run reads a non-empty sdk_version, the GUID is unchanged (SC-102, SC-106).
    ''' </summary>
    <Fact>
    Public Sub VersionOneMapUpgradesInPlace()
        Using map As TempMap = New TempMap()
            V1MapFixture.CopyToTemp(map)
            Assert.Equal(1, MapQueries.ReadSchemaVersion(map.Path))
            Dim guidBefore As String = MapQueries.ReadMapGuid(map.Path)
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim before As Dictionary(Of String, Long) = Counts(map.Path, solutionId)
            Dim runsBefore As Long = before("extract_runs")

            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))

            Assert.Equal(3, MapQueries.ReadSchemaVersion(map.Path))
            Assert.Equal(guidBefore, MapQueries.ReadMapGuid(map.Path))
            Dim after As Dictionary(Of String, Long) = Counts(map.Path, solutionId)
            Assert.Equal(before("map_identity"), after("map_identity"))
            Assert.Equal(before("solutions"), after("solutions"))
            Assert.Equal(runsBefore + 1, after("extract_runs"))
            Assert.Equal(before("code_symbols"), after("code_symbols"))
            Assert.Equal(before("code_parts"), after("code_parts"))
            Assert.Equal(before("rename_candidates"), after("rename_candidates"))
            ' Fixpack 003: code_edges follows the extractor (rule 2 writes the fixture's bare-name occurrences the Stage A run did not); the
            ' upgraded map holds exactly what a fresh map of the same fixture gets from this executable.
            Using fresh As TempMap = New TempMap()
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = fresh.Path}, Nothing))
                Assert.Equal(MapQueries.CountRows(fresh.Path, "code_edges", MapQueries.ReadSolutions(fresh.Path)(0).Id), after("code_edges"))
            End Using

            Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
            Assert.Equal(CInt(runsBefore) + 1, runs.Count)
            For i As Integer = 0 To CInt(runsBefore) - 1
                Assert.Null(runs(i).SdkVersion)
                Assert.Equal(1, runs(i).SchemaVersion)
            Next
            Dim newest As RunRow = runs(runs.Count - 1)
            Assert.Equal("completed", newest.Outcome)
            Assert.Equal(3, newest.SchemaVersion)
            Assert.False(String.IsNullOrEmpty(newest.SdkVersion), "sdk_version is empty on the upgrading run")
            Assert.Equal(before("code_symbols"), CLng(newest.SymbolsMatched))
        End Using
    End Sub

    ''' <summary>
    ''' (2) one shape: sqlite_master (type, name, tbl_name, sql) of an upgraded map equals that of a map created fresh (research R22, FR-112).
    ''' </summary>
    <Fact>
    Public Sub UpgradedMapHasTheSameSchemaObjectsAsAFreshMap()
        Using upgraded As TempMap = New TempMap()
            Using fresh As TempMap = New TempMap()
                V1MapFixture.CopyToTemp(upgraded)
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = upgraded.Path}, Nothing))
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = fresh.Path}, Nothing))
                Dim left As List(Of String) = MapQueries.ReadSchemaObjects(upgraded.Path)
                Dim right As List(Of String) = MapQueries.ReadSchemaObjects(fresh.Path)
                Assert.Equal(right, left)
                Assert.Contains(left, Function(l As String) l.StartsWith("trigger|tr_extract_runs_sdk_version_insert|", StringComparison.Ordinal))
                Assert.Contains(left, Function(l As String) l.StartsWith("trigger|tr_extract_runs_sdk_version_update|", StringComparison.Ordinal))
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (3) a map at version 4 is refused with exit 1 and nothing written: no run row, no schema object added (FR-114).
    ''' </summary>
    <Fact>
    Public Sub NewerMapIsRefusedUntouched()
        Using map As TempMap = New TempMap()
            V1MapFixture.CopyToTemp(map)
            MapQueries.SetSchemaVersion(map.Path, 4)
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim runsBefore As Long = MapQueries.CountRows(map.Path, "extract_runs", solutionId)
            Dim objectsBefore As List(Of String) = MapQueries.ReadSchemaObjects(map.Path)
            Assert.Equal(ExitCode.Failure, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Assert.Equal(4, MapQueries.ReadSchemaVersion(map.Path))
            Assert.Equal(runsBefore, MapQueries.CountRows(map.Path, "extract_runs", solutionId))
            Assert.Equal(objectsBefore, MapQueries.ReadSchemaObjects(map.Path))
        End Using
    End Sub

    ''' <summary>
    ''' (4) an abort after the migration statements and before schema_version is set (DuringUpgrade, armed with the nonce) leaves the map at
    ''' version 1 with its schema objects and row counts unchanged; the next run upgrades and completes (FR-113, SC-104).
    ''' </summary>
    <Fact>
    Public Sub AbortDuringUpgradeLeavesVersionOneIntactAndTheNextRunUpgrades()
        Using map As TempMap = New TempMap()
            V1MapFixture.CopyToTemp(map)
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim objectsBefore As List(Of String) = MapQueries.ReadSchemaObjects(map.Path)
            Dim before As Dictionary(Of String, Long) = Counts(map.Path, solutionId)

            Dim nonce As String = Guid.NewGuid().ToString("N")
            Dim environment As Dictionary(Of String, String) = New Dictionary(Of String, String) From {{"CODEMEM_TEST_ABORT_AT", "DuringUpgrade:" & nonce}, {"CODEMEM_TEST_NONCE", nonce}}
            Dim aborted As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path), environment)
            Assert.NotEqual(0, aborted.ExitCode)
            Assert.Contains("CODEMEM_TEST_ABORT_AT=DuringUpgrade", aborted.StandardError)

            Assert.Equal(1, MapQueries.ReadSchemaVersion(map.Path))
            Assert.Equal(objectsBefore, MapQueries.ReadSchemaObjects(map.Path))
            Dim after As Dictionary(Of String, Long) = Counts(map.Path, solutionId)
            For Each table As String In before.Keys
                Assert.True(before(table) = after(table), table & " changed after the aborted upgrade")
            Next

            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Assert.Equal(3, MapQueries.ReadSchemaVersion(map.Path))
            Assert.Equal(before("extract_runs") + 1, MapQueries.CountRows(map.Path, "extract_runs", solutionId))
        End Using
    End Sub

    Private Shared Function Counts(db As String, solutionId As Long) As Dictionary(Of String, Long)
        Dim result As Dictionary(Of String, Long) = New Dictionary(Of String, Long)(StringComparer.Ordinal)
        For Each table As String In New String() {"map_identity", "solutions", "extract_runs", "code_symbols", "code_parts", "code_edges", "rename_candidates"}
            result(table) = MapQueries.CountRows(db, table, solutionId)
        Next
        Return result
    End Function

End Class

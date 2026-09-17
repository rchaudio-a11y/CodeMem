' File: S03_SchemaVersion3Tests.vb
' Project: CodeMem.Tests
' Description: Schema version 3 (feature 005, FR-429, FR-430; research R63): a fresh map is created at 3 with the warning table; version-1 and version-2 maps upgrade in place; an abort during the upgrade rolls back; fresh and upgraded schemas are one shape.
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' (4) as S02 (4) taught (2026-09-10): the whole upgrade sits inside the BEGIN IMMEDIATE the abort discards, so an aborted 2 -> 3 leaves the
' map at 2 with its schema objects unchanged - the table is not "present but unversioned" (the tasks' first wording, corrected here at T007).
' RED:   2026-09-17 (T007) 5 of 5 for their stated reasons - (1), (2) expected 3, actual 2; (3) actual 2 (a version-2 map is Current today,
'        nothing upgrades it); (4) the abort never fired (exit 0: no upgrade step runs on a Current map, so no DuringUpgrade point is reached);
'        (5) no table|extract_run_warnings line in any map.
' GREEN: 2026-09-17 (T009) 5 of 5 after Current = 3, SchemaState.Version2, MigrationToVersion3 / UpgradeToVersion3, InspectSchema and the
'        three upgrade paths of ExtractionRun step 4; F06, S02 (1) (4) and US1 went red as named (2 -> 3, and the run row's own
'        schema_version) and were amended in place; S02 (3) bumps to 4 now that 3 is current.
' FIRE:  2026-09-17 (T009) (4): the tasks' first fire (AbortIf after SetSchemaVersion) cannot go red - both sit in the one transaction the
'        abort discards (S02 (4)'s 2026-09-10 finding). Substitute fire, as S02: db.Commit() : db.BeginImmediate() before the abort in the
'        Version2 path -> red (Collections differ: the table survived the abort); reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Runs the extractor against a fresh map, a copy of Fixtures/Maps/sample-v1.sqlite and a copy of Fixtures/Maps/version2.sqlite.
''' </summary>
<Collection("Fixture")>
Public Class S03_SchemaVersion3Tests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' (1) a fresh map is at version 3 and holds extract_run_warnings and its index (FR-429).
    ''' </summary>
    <Fact>
    Public Sub AFreshMapIsVersionThreeWithTheWarningTable()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Assert.Equal(3, MapQueries.ReadSchemaVersion(map.Path))
            AssertHoldsTheWarningTable(MapQueries.ReadSchemaObjects(map.Path))
        End Using
    End Sub

    ''' <summary>
    ''' (2) a version-1 map upgrades to 3 in place: the GUID and the identity rows stay, one run is added.
    ''' </summary>
    <Fact>
    Public Sub AVersionOneMapUpgradesToThree()
        Using map As TempMap = New TempMap()
            V1MapFixture.CopyToTemp(map)
            Assert.Equal(1, MapQueries.ReadSchemaVersion(map.Path))
            AssertUpgradesInPlace(map)
        End Using
    End Sub

    ''' <summary>
    ''' (3) a version-2 map upgrades to 3 in place: the GUID and the identity rows stay, one run is added.
    ''' </summary>
    <Fact>
    Public Sub AVersionTwoMapUpgradesToThree()
        Using map As TempMap = New TempMap()
            V2MapFixture.CopyToTemp(map)
            Assert.Equal(2, MapQueries.ReadSchemaVersion(map.Path))
            AssertUpgradesInPlace(map)
        End Using
    End Sub

    ''' <summary>
    ''' (4) an abort after the migration statements and before schema_version is set (DuringUpgrade, armed with the nonce) leaves a version-2
    ''' map at 2 with its schema objects and row counts unchanged (the transaction is discarded whole); the next run upgrades and completes.
    ''' </summary>
    <Fact>
    Public Sub AbortDuringUpgradeOnAVersionTwoMapLeavesItAtTwo()
        Using map As TempMap = New TempMap()
            V2MapFixture.CopyToTemp(map)
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim objectsBefore As List(Of String) = MapQueries.ReadSchemaObjects(map.Path)
            Dim before As Dictionary(Of String, Long) = Counts(map.Path, solutionId)

            Dim nonce As String = Guid.NewGuid().ToString("N")
            Dim environment As Dictionary(Of String, String) = New Dictionary(Of String, String) From {{"CODEMEM_TEST_ABORT_AT", "DuringUpgrade:" & nonce}, {"CODEMEM_TEST_NONCE", nonce}}
            Dim aborted As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path), environment)
            Assert.NotEqual(0, aborted.ExitCode)
            Assert.Contains("CODEMEM_TEST_ABORT_AT=DuringUpgrade", aborted.StandardError)

            Assert.Equal(2, MapQueries.ReadSchemaVersion(map.Path))
            Assert.Equal(objectsBefore, MapQueries.ReadSchemaObjects(map.Path))
            Dim after As Dictionary(Of String, Long) = Counts(map.Path, solutionId)
            For Each table As String In before.Keys
                Assert.True(before(table) = after(table), table & " changed after the aborted upgrade")
            Next

            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Assert.Equal(3, MapQueries.ReadSchemaVersion(map.Path))
            AssertHoldsTheWarningTable(MapQueries.ReadSchemaObjects(map.Path))
            Assert.Equal(before("extract_runs") + 1, MapQueries.CountRows(map.Path, "extract_runs", solutionId))
        End Using
    End Sub

    ''' <summary>
    ''' (5) one shape: sqlite_master (type, name, tbl_name, sql) of a fresh map, a map upgraded from 1 and a map upgraded from 2 are
    ''' identical, and each holds the warning table (research R22 extended to version 3).
    ''' </summary>
    <Fact>
    Public Sub FreshAndUpgradedSchemasAreByteIdentical()
        Using fresh As TempMap = New TempMap()
            Using fromOne As TempMap = New TempMap()
                Using fromTwo As TempMap = New TempMap()
                    V1MapFixture.CopyToTemp(fromOne)
                    V2MapFixture.CopyToTemp(fromTwo)
                    For Each path As String In New String() {fresh.Path, fromOne.Path, fromTwo.Path}
                        Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = path}, Nothing))
                    Next
                    Dim expected As List(Of String) = MapQueries.ReadSchemaObjects(fresh.Path)
                    AssertHoldsTheWarningTable(expected)
                    Assert.Equal(expected, MapQueries.ReadSchemaObjects(fromOne.Path))
                    Assert.Equal(expected, MapQueries.ReadSchemaObjects(fromTwo.Path))
                End Using
            End Using
        End Using
    End Sub

    Private Sub AssertUpgradesInPlace(map As TempMap)
        Dim guidBefore As String = MapQueries.ReadMapGuid(map.Path)
        Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
        Dim before As Dictionary(Of String, Long) = Counts(map.Path, solutionId)
        Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
        Assert.Equal(3, MapQueries.ReadSchemaVersion(map.Path))
        Assert.Equal(guidBefore, MapQueries.ReadMapGuid(map.Path))
        AssertHoldsTheWarningTable(MapQueries.ReadSchemaObjects(map.Path))
        Dim after As Dictionary(Of String, Long) = Counts(map.Path, solutionId)
        Assert.Equal(before("map_identity"), after("map_identity"))
        Assert.Equal(before("solutions"), after("solutions"))
        Assert.Equal(before("extract_runs") + 1, after("extract_runs"))
        Assert.Equal(before("code_symbols"), after("code_symbols"))
    End Sub

    Private Shared Sub AssertHoldsTheWarningTable(objects As List(Of String))
        Assert.Contains(objects, Function(l As String) l.StartsWith("table|extract_run_warnings|", StringComparison.Ordinal))
        Assert.Contains(objects, Function(l As String) l.StartsWith("index|ix_extract_run_warnings_run_id|", StringComparison.Ordinal))
    End Sub

    Private Shared Function Counts(db As String, solutionId As Long) As Dictionary(Of String, Long)
        Dim result As Dictionary(Of String, Long) = New Dictionary(Of String, Long)(StringComparer.Ordinal)
        For Each table As String In New String() {"map_identity", "solutions", "extract_runs", "code_symbols", "code_parts", "code_edges", "rename_candidates"}
            result(table) = MapQueries.CountRows(db, table, solutionId)
        Next
        Return result
    End Function

End Class

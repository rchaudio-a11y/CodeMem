' File: RefusalTests.vb
' Project: CodeMem.Tests
' Description: The exit-1 refusals of contracts/cli.md and the FR-033 summary line, proven through the production route and the executable.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 not red: the exit-1 mappings landed with Slice A; recorded as the production-route proof of each refusal.
' GREEN: 2026-09-09 first run, 4 of 4.
'
' 2026-09-10 (fixpack 002, FR-114): the schema-mismatch fact stamps version 3 through SetSchemaVersion (99 also refuses; 3 documents "greater
' than 2" now that Current is 2 and version 1 is upgraded rather than refused). Still exit 1, still one run row.
' 2026-09-17 (feature 005, T010; a Red the plan did not name - the same shape as S02 (3)): red once Current became 3 - the map stamped 3 is
' now current, proceeds, and the assertions no longer describe a refusal. Stamps 4 now (greater than Current = 3). Re-run -> green.
' RED:   2026-09-17 (feature 005, T035, the named Red - analyze G2): (d) red the moment warnings=<n> joined the summary line ("Pattern not
'        found in value": the anchored regex ended at sha=). Amended: the pattern gains warnings=\d+ before its anchor (contracts/extractor.md §4).

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Schema mismatch, usage error, missing db directory, and the exact stdout contract on success.
''' </summary>
<Collection("Fixture")>
Public Class RefusalTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' (a) a map stamped with schema version 4 (greater than Current = 3) is refused with exit 1 and no new run row.
    ''' </summary>
    <Fact>
    Public Sub SchemaVersionMismatchIsRefused()
        Using map As TempMap = New TempMap()
            Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
            MapQueries.SetSchemaVersion(map.Path, 4)
            Assert.Equal(ExitCode.Failure, ExtractionRun.Execute(options, Nothing))
            Assert.Single(MapQueries.ReadRuns(map.Path))
        End Using
    End Sub

    ''' <summary>
    ''' (b) no arguments: exit 1 and usage on stderr.
    ''' </summary>
    <Fact>
    Public Sub NoArgumentsIsAUsageError()
        Dim run As ExtractorProcess = ExtractorProcess.Run("")
        Assert.Equal(1, run.ExitCode)
        Assert.Contains("usage:", run.StandardError)
        Assert.Equal("", run.StandardOutput)
    End Sub

    ''' <summary>
    ''' (c) a --db path in a directory that does not exist is a usage error (exit 1), not an unobtainable lock (FR-034).
    ''' </summary>
    <Fact>
    Public Sub MissingDbDirectoryIsAUsageError()
        Dim missing As String = IO.Path.Combine(IO.Path.GetTempPath(), "codemem-tests", "does-not-exist-" & Guid.NewGuid().ToString("N"), "map.sqlite")
        Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(missing))
        Assert.Equal(1, run.ExitCode)
        Assert.Equal("", run.StandardOutput)
    End Sub

    ''' <summary>
    ''' (d) a successful run prints exactly one stdout line in the FR-033 shape.
    ''' </summary>
    <Fact>
    Public Sub SuccessPrintsExactlyOneSummaryLine()
        Using map As TempMap = New TempMap()
            Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path))
            Assert.Equal(0, run.ExitCode)
            Dim lines As String() = run.StandardOutput.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Assert.Single(lines)
            Assert.Matches("^solution=\S+ run_id=\d+ observed=\d+ matched=\d+ reactivated=\d+ new=\d+ retired=\d+ registry_before=\d+ notes_orphaned=\d+ candidates=\d+ unaccounted_observed=-?\d+ unaccounted_registry=-?\d+ digest=[0-9a-f]{64} sha=([0-9a-f]{40}|null) warnings=\d+$", lines(0))
        End Using
    End Sub

End Class

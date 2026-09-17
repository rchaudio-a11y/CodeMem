' File: X02_WarningTests.vb
' Project: CodeMem.Tests
' Description: US5 (feature 005): a NuGet restore warning is recorded on the run and does not fail the load; a compile error still exits 2; a restore error stops the load by name before the compiler; NoWarn in the environment is inert; the bridge reports the warnings; an unmatched failure still aborts; warnings are written beside a failed run row (FR-427-FR-430; Q8 as ruled; R62, R71; STOP 1 decision 3).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Extractions run through the executable so the child's environment is controlled: the launch passes removals for every NoWarn* key of
' the test process, asserted before the launch (ChildEnvironment). (1) and (5) share Nu1701MapFixture's one launch; the others launch by
' design (analyze I3). The copies a fact edits are WarningsCopy temp copies restored outside the tree.
' RED:   2026-09-17 (T033) 6 of 7 on the first build: (1), (2) and (5) exit 1 "workspace load failed: | Msbuild failed when processing the file
'        '...Nu1701.vbproj' with message: Package 'CodeMem.NetFxOnly 1.0.0' was restored using '.NETFramework,Version=v4.6.1, ...'" - the
'        loader's any-Failure-aborts rule, the compiler never reached; (3) MSBuild's text where "restore error NU1101" was expected; (4) the
'        exit codes differ (1 without NoWarn, 0 with NoWarn=NU1701 in the child's environment - MSBuild honours the variable as a global
'        property, see the T034 note); (7) exit 1 where 4 was expected. (6) green: an unmatched failure aborts today as it will tomorrow.
' 2026-09-17 (T034) (2), (3), (6) green with AssetsLog and the loader's rule; (1), (7) red on the missing rows (T035), (5) on the missing
'        envelope (T036). (T035) (1), (2), (3), (6), (7) green with the rows and warnings=<n>; (4) red again - and for the opposite reason
'        its premise assumed: with NoWarn=NU1701 in the child's environment the run exits 0 with 0 rows. MSBuild honours the variable
'        (the SDK folds $(NoWarn) into MSBuildWarningsAsMessages), so the replay never reaches the loader. (4) rewritten to assert that
'        observation and the contrast with the clean launch - a deviation from the task's "the variable is inert", recorded for the
'        Architect (quickstart, T037).
' GREEN: 2026-09-17 (T036) 7 of 7 with ReadByRun, run.warnings and latestRun.warnings: (5) reads the NU1701 row through the bridge on the
'        shared map and through one real launch of the door.
' FIRE:  2026-09-17 (T036) (3): the loader made to treat every matched assets-log entry as a warning (the level test dropped) -> red
'        "exit 0": NU1101 continued to the compiler and a run was published; reverted from a byte copy -> 7 of 7.


Imports System.IO
Imports System.Text.Json
Imports CodeMem.Bridging
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The Warnings collection restores Nu1701 and Nu1101 once; nothing here touches the committed fixtures.
''' </summary>
<Collection("Warnings")>
Public Class X02_WarningTests
    Implements IClassFixture(Of Nu1701MapFixture)

    Private ReadOnly _fixture As WarningsFixture
    Private ReadOnly _nu1701 As Nu1701MapFixture

    ''' <summary>
    ''' Receives the restored fixtures and the shared launch.
    ''' </summary>
    ''' <param name="fixture">The collection fixture.</param>
    ''' <param name="nu1701">The class fixture.</param>
    Public Sub New(fixture As WarningsFixture, nu1701 As Nu1701MapFixture)
        _fixture = fixture
        _nu1701 = nu1701
    End Sub

    ''' <summary>
    ''' (1) A NuGet warning (NU1701, a .NET Framework-only package on net8.0) is recorded on the run and the load continues (FR-427): exit 0,
    ''' one extract_run_warnings row with the code and the project's solution-relative path, the summary line ending warnings=1.
    ''' </summary>
    <Fact>
    Public Sub ANuGetWarningIsRecordedAndTheLoadContinues()
        Dim run As ExtractorProcess = _nu1701.Run
        Assert.True(run.ExitCode = 0, "exit " & run.ExitCode & ": " & run.StandardError)
        Dim runs As List(Of RunRow) = MapQueries.ReadRuns(_nu1701.Map.Path)
        Assert.Single(runs)
        Dim warnings As List(Of (Code As String, ProjectPath As String, Message As String)) = MapQueries.ReadWarnings(_nu1701.Map.Path, runs(0).Id)
        Assert.True(warnings.Count >= 1, "no warning row")
        Assert.Contains(warnings, Function(w As (Code As String, ProjectPath As String, Message As String)) w.Code = "NU1701" AndAlso w.ProjectPath = "Nu1701.vbproj")
        Dim summary As String = SummaryOf(run)
        Assert.EndsWith(" warnings=" & warnings.Count, summary)
    End Sub

    ''' <summary>
    ''' (2) A compile error still exits 2 (FR-428): a copy of Nu1701 with Broken.vb - errors= on stderr, no run row, no warning row.
    ''' </summary>
    <Fact>
    Public Sub ACompileErrorStillExitsTwo()
        Using copy As WarningsCopy = New WarningsCopy(_fixture.Nu1701ProjectPath)
            IO.File.WriteAllText(Path.Combine(copy.Directory, "Broken.vb"), "Public Class Broken" & vbLf & "    Inherits Nothing" & vbLf & "End Class" & vbLf)
            Assert.Equal(0, copy.Restore())
            Using map As TempMap = New TempMap()
                Dim run As ExtractorProcess = Launch(copy.ProjectPath, map)
                Assert.True(run.ExitCode = 2, "exit " & run.ExitCode & ": " & run.StandardError)
                Assert.Contains("errors=", run.StandardError, StringComparison.Ordinal)
                Assert.True(HoldsNoRun(map.Path), "a run was written")
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (3) A restore error (NU1101, a package no source holds) stops the load by name before the compiler (FR-427; Q8): exit 1, the first
    ''' stderr line naming the code and the project, no errors= line anywhere, no run row.
    ''' </summary>
    <Fact>
    Public Sub ARestoreErrorStopsTheLoadByNameBeforeTheCompiler()
        Using map As TempMap = New TempMap()
            Dim run As ExtractorProcess = Launch(_fixture.Nu1101ProjectPath, map)
            Assert.True(run.ExitCode = 1, "exit " & run.ExitCode & ": " & run.StandardError)
            Dim first As String = FirstLine(run.StandardError)
            Assert.Contains("restore error NU1101", first, StringComparison.Ordinal)
            Assert.Contains("Nu1101.vbproj", first, StringComparison.Ordinal)
            Assert.DoesNotContain("errors=", run.StandardError, StringComparison.Ordinal)
            Assert.True(HoldsNoRun(map.Path), "a run was written")
        End Using
    End Sub

    ''' <summary>
    ''' (4) NoWarn=NU1701 in the child's environment is MSBuild's business, not the extractor's (FR-427 as observed at T035): MSBuild takes the
    ''' variable as a global property and NuGet's replay is suppressed before the loader sees a Failure - exit 0, no warning row, warnings=0 on
    ''' the summary line - while (1)'s clean launch records the row. The extractor reads no such variable and passes no NoWarn; that is why every
    ''' launch of this suite scrubs NoWarn* first (ChildEnvironment) and why an Operator's shell must not carry the 152705 workaround.
    ''' </summary>
    <Fact>
    Public Sub NoWarnInTheEnvironmentIsMSBuildsNotTheExtractors()
        Assert.True(_nu1701.Run.ExitCode = 0, "the clean launch failed: " & _nu1701.Run.StandardError)
        Dim cleanRows As Integer = MapQueries.ReadWarnings(_nu1701.Map.Path, MapQueries.ReadRuns(_nu1701.Map.Path)(0).Id).Count
        Assert.True(cleanRows >= 1, "the clean launch recorded no warning")
        Using map As TempMap = New TempMap()
            Dim environment As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {{"NoWarn", "NU1701"}}
            Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_nu1701.ProjectPath) & " --db " & ExtractorProcess.Quote(map.Path), environment)
            Assert.True(run.ExitCode = 0, "exit " & run.ExitCode & ": " & run.StandardError)
            Assert.Empty(MapQueries.ReadWarnings(map.Path, MapQueries.ReadRuns(map.Path)(0).Id))
            Assert.EndsWith(" warnings=0", SummaryOf(run))
        End Using
    End Sub

    ''' <summary>
    ''' (5) The bridge reports the warnings (FR-430): solutions carries latestRun.warnings = 1 on the shared map; extract by key with the
    ''' project's path, one real launch, answers run.warnings with the NU1701 row.
    ''' </summary>
    <Fact>
    Public Sub TheBridgeReportsTheWarnings()
        Assert.True(_nu1701.Run.ExitCode = 0, "the shared launch failed: " & _nu1701.Run.StandardError)
        Dim config As String = BridgeHost.WriteConfig(_nu1701.Map.Path, Nothing, True, True)
        Dim host As BridgeHost = New BridgeHost(config)
        Dim reply As BridgeReply = host.Invoke("solutions", Args())
        Assert.False(reply.IsError, reply.Text)
        Dim solution As JsonElement = Assert.Single(reply.Root().GetProperty("solutions").EnumerateArray())
        Assert.Equal("Nu1701", solution.GetProperty("key").GetString())
        Assert.Equal(1, solution.GetProperty("latestRun").GetProperty("warnings").GetInt32())
        Dim door As ExtractDoor = New ExtractDoor(config, New ProcessExtractorLauncher())
        Dim result As ExtractResult = door.Run(New ExtractRequest With {.Origin = ExtractOrigin.Tool, .SolutionKey = "Nu1701", .SolutionPath = _nu1701.ProjectPath})
        Assert.True(result.Launched AndAlso CInt(result.ExitCode) = 0, "not run: " & If(result.Refusal Is Nothing, If(result.Line, ""), result.Refusal.Text))
        Assert.NotNull(result.Run)
        Using document As JsonDocument = JsonDocument.Parse(BridgeJson.Serialize(result))
            Dim warning As JsonElement = Assert.Single(document.RootElement.GetProperty("run").GetProperty("warnings").EnumerateArray())
            Assert.Equal("NU1701", warning.GetProperty("code").GetString())
            Assert.Equal("Nu1701.vbproj", warning.GetProperty("projectPath").GetString())
        End Using
    End Sub

    ''' <summary>
    ''' (6) A workspace failure the assets log does not explain still aborts as today (FR-427's third row): a copy whose project references
    ''' a project that does not exist - exit 1, "workspace load failed" with MSBuild's text, no "restore error".
    ''' </summary>
    <Fact>
    Public Sub AnUnmatchedFailureStillAborts()
        Using copy As WarningsCopy = New WarningsCopy(_fixture.Nu1701ProjectPath)
            Dim project As String = IO.File.ReadAllText(copy.ProjectPath)
            project = project.Replace("</Project>", "  <ItemGroup>" & vbLf & "    <ProjectReference Include=""..\Nowhere\Nowhere.vbproj"" />" & vbLf & "  </ItemGroup>" & vbLf & "</Project>")
            IO.File.WriteAllText(copy.ProjectPath, project)
            copy.Restore()
            Using map As TempMap = New TempMap()
                Dim run As ExtractorProcess = Launch(copy.ProjectPath, map)
                Assert.True(run.ExitCode = 1, "exit " & run.ExitCode & ": " & run.StandardError)
                Assert.Contains("workspace load failed", run.StandardError, StringComparison.Ordinal)
                Assert.DoesNotContain("restore error", run.StandardError, StringComparison.Ordinal)
                Assert.True(HoldsNoRun(map.Path), "a run was written")
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (7) Warnings are written beside a failed run row too (FR-429): the residual seam turns the Nu1701 run into exit 4 and its failed
    ''' row carries the NU1701 row.
    ''' </summary>
    <Fact>
    Public Sub WarningsAreWrittenBesideAFailedRunRow()
        Using map As TempMap = New TempMap()
            Dim seams As RunSeams = New RunSeams With {.CorruptStagedCounts = Sub(c As Core.RunCounts) c.SymbolsMatched += 1}
            Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _nu1701.ProjectPath, .DbPath = map.Path}, seams)
            Assert.Equal(ExitCode.ResidualMismatch, code)
            Dim failed As RunRow = Assert.Single(MapQueries.ReadRuns(map.Path))
            Assert.Equal("failed", failed.Outcome)
            Dim warnings As List(Of (Code As String, ProjectPath As String, Message As String)) = MapQueries.ReadWarnings(map.Path, failed.Id)
            Assert.Contains(warnings, Function(w As (Code As String, ProjectPath As String, Message As String)) w.Code = "NU1701")
        End Using
    End Sub

    Private Shared Function Launch(projectPath As String, map As TempMap) As ExtractorProcess
        Dim environment As Dictionary(Of String, String) = ChildEnvironment.WithoutNoWarn()
        ChildEnvironment.AssertNoNoWarn(environment)
        Return ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(projectPath) & " --db " & ExtractorProcess.Quote(map.Path), environment)
    End Function

    Private Shared Function SummaryOf(run As ExtractorProcess) As String
        Dim lines As String() = run.StandardOutput.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        Assert.Single(lines)
        Return lines(0)
    End Function

    Private Shared Function FirstLine(text As String) As String
        Dim lines As String() = text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        Return If(lines.Length > 0, lines(0), "")
    End Function

    Private Shared Function HoldsNoRun(mapPath As String) As Boolean
        If Not IO.File.Exists(mapPath) Then Return True
        Try
            Return MapQueries.ReadRuns(mapPath).Count = 0
        Catch ex As Microsoft.Data.Sqlite.SqliteException
            Return True
        End Try
    End Function

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

End Class

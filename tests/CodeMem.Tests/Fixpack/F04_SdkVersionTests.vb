' File: F04_SdkVersionTests.vb
' Project: CodeMem.Tests
' Description: F4 (sdk half) - every run row written at schema version 2 stamps the SDK version the host resolver selects for the solution directory, completed or failed (FR-109, SC-106).
' Author: RCH Automation LLC
' Created: 2026-09-10
'
' RED:   2026-09-10 both facts red: SQLite Error 1 'no such column: sdk_version' (the column did not exist at schema version 1).
' GREEN: 2026-09-10 after SdkVersion.Resolve (hostfxr_resolve_sdk2 in-process, research R21) and the version-2 schema: 2 of 2; the stamp read
'        10.0.401, equal to dotnet --version in the fixture directory; the exit-4 failed row carried it too.
' FIRE:  2026-09-10 Resolve returned RuntimeInformation.FrameworkDescription -> red (expected "10.0.401", actual ".NET 8.0.31"); reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The stamp equals dotnet --version in the fixture directory (a dev-time SDK use, Article XV) and is present on the exit-4 failed row too.
''' </summary>
<Collection("Fixture")>
Public Class F04_SdkVersionTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' A completed run's sdk_version is non-empty and equals the trimmed output of dotnet --version run in the fixture directory.
    ''' </summary>
    <Fact>
    Public Sub CompletedRunStampsTheResolvedSdkVersion()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim run As RunRow = MapQueries.ReadRuns(map.Path)(0)
            Assert.False(String.IsNullOrEmpty(run.SdkVersion), "sdk_version is empty")
            Assert.Equal(DotnetCli.Output("--version", _fixture.FixtureDirectory), run.SdkVersion)
        End Using
    End Sub

    ''' <summary>
    ''' The failed row of an exit-4 run (the CorruptStagedCounts seam) also carries a non-null sdk_version.
    ''' </summary>
    <Fact>
    Public Sub FailedRunStampsTheSdkVersionToo()
        Using map As TempMap = New TempMap()
            Dim seams As RunSeams = New RunSeams With {.CorruptStagedCounts = Sub(c As Core.RunCounts) c.SymbolsMatched += 1}
            Assert.Equal(ExitCode.ResidualMismatch, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, seams))
            Dim run As RunRow = MapQueries.ReadRuns(map.Path)(0)
            Assert.Equal("failed", run.Outcome)
            Assert.False(String.IsNullOrEmpty(run.SdkVersion), "sdk_version is empty on the failed row")
        End Using
    End Sub

End Class

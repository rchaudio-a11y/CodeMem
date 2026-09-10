' File: I14_MapIdentityTests.vb
' Project: CodeMem.Tests
' Description: I14 - the map GUID is identical after runs 1, 2 and 3 (SC-011).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   n/a - expected green from OpenOrCreate; the guard is trusted through its FIRE.
' GREEN: 2026-09-09 first run.
' FIRE:  2026-09-09 made OpenOrCreate insert the identity row on an existing file -> red (run 2 fails on CHECK id = 1); reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' map_identity is written once and never rewritten (Article IX).
''' </summary>
<Collection("Fixture")>
Public Class I14_MapIdentityTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' Three runs, one GUID.
    ''' </summary>
    <Fact>
    Public Sub GuidSurvivesThreeRuns()
        Using map As TempMap = New TempMap()
            Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
            Dim first As String = MapQueries.ReadMapGuid(map.Path)
            Assert.Equal(36, first.Length)
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
            Assert.Equal(first, MapQueries.ReadMapGuid(map.Path))
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
            Assert.Equal(first, MapQueries.ReadMapGuid(map.Path))
            Assert.Equal(1L, MapQueries.CountRows(map.Path, "map_identity", 0))
        End Using
    End Sub

End Class

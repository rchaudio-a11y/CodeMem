' File: F05_ConnectionStringTests.vb
' Project: CodeMem.Tests
' Description: F5 - a --db path containing ';' opens exactly that file: the path is a typed connection-string value, never delimited text (FR-104).
' Author: RCH Automation LLC
' Created: 2026-09-10
'
' RED:   2026-09-10 not red: the typed connection string landed with Slice A (T023) before this file first ran; green on first run.
' GREEN: 2026-09-10 first run: the map sits at map-<guid>;semi.sqlite, nothing at the text before the ';', one completed run.
' FIRE:  2026-09-10 (T031) MapDatabase.Open rebuilt the connection string by concatenation ("Data Source=" & path & ";Pooling=False") -> red
'        (Execute returned Failure: the ';' split the string); reverted -> green.

Imports System.IO
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' A map at map-&lt;guid&gt;;semi.sqlite is created and populated at that path and nowhere else.
''' </summary>
<Collection("Fixture")>
Public Class F05_ConnectionStringTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' Exit 0; the file exists at exactly map.Path; nothing exists at the text before the ';'; one completed run is readable.
    ''' </summary>
    <Fact>
    Public Sub SemicolonPathOpensExactlyThatFile()
        Using map As TempMap = New TempMap(";semi")
            Dim truncated As String = map.Path.Substring(0, map.Path.IndexOf(";"c))
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Assert.True(File.Exists(map.Path), "no file at the ';' path")
            Assert.False(File.Exists(truncated), "a file was created at the text before the ';'")
            Assert.False(File.Exists(truncated & ".sqlite"), "a file was created at the text before the ';' plus .sqlite")
            Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
            Assert.Single(runs)
            Assert.Equal("completed", runs(0).Outcome)
        End Using
    End Sub

End Class

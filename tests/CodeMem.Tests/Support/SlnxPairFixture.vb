' File: SlnxPairFixture.vb
' Project: CodeMem.Tests
' Description: Class fixture for X01 (feature 005, T030): the committed fixture extracted twice into two fresh maps, once through Sample.sln and once through Sample.slnx, the exit codes kept rather than thrown so a refused .slnx is a red fact, not an errored class.
' Author: RCH Automation LLC
' Created: 2026-09-17

Imports System.IO
Imports CodeMem.Extraction

''' <summary>
''' Both extractions run in-process through ExtractionRun.Execute; the Fixture collection has restored the fixture before this is built.
''' </summary>
Public Class SlnxPairFixture
    Implements IDisposable

    ''' <summary>The map Sample.sln was extracted into.</summary>
    Public ReadOnly Property SlnMap As TempMap

    ''' <summary>The map Sample.slnx was extracted into.</summary>
    Public ReadOnly Property SlnxMap As TempMap

    ''' <summary>The .sln extraction's exit code.</summary>
    Public ReadOnly Property SlnExit As ExitCode

    ''' <summary>The .slnx extraction's exit code.</summary>
    Public ReadOnly Property SlnxExit As ExitCode

    ''' <summary>The fixture's Sample.sln.</summary>
    Public ReadOnly Property SlnPath As String

    ''' <summary>The fixture's Sample.slnx.</summary>
    Public ReadOnly Property SlnxPath As String

    ''' <summary>
    ''' Extracts both.
    ''' </summary>
    Public Sub New()
        Dim fixture As String = RepoPaths.FixtureDirectory()
        SlnPath = Path.Combine(fixture, "Sample.sln")
        SlnxPath = Path.Combine(fixture, "Sample.slnx")
        SlnMap = New TempMap()
        SlnxMap = New TempMap()
        SlnExit = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = SlnPath, .DbPath = SlnMap.Path}, Nothing)
        SlnxExit = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = SlnxPath, .DbPath = SlnxMap.Path}, Nothing)
    End Sub

    ''' <summary>
    ''' The one solution's id in a map of this pair.
    ''' </summary>
    ''' <param name="map">SlnMap or SlnxMap.</param>
    ''' <returns>The id.</returns>
    Public Function SolutionId(map As TempMap) As Long
        Return MapQueries.ReadSolutions(map.Path)(0).Id
    End Function

    ''' <summary>
    ''' Deletes both maps.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        SlnxMap.Dispose()
        SlnMap.Dispose()
    End Sub

End Class

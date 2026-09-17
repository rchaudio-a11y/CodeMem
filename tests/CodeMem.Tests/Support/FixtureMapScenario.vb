' File: FixtureMapScenario.vb
' Project: CodeMem.Tests
' Description: Class fixture for the bridge's read facts: the committed fixture solution extracted once into a temp map, a configuration and an in-process host (feature 004).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T017): no registry and no store path - the map is the only file the configuration names (FR-403, FR-433).

Imports System.IO
Imports CodeMem.Extraction

''' <summary>
''' One extraction per test class instead of one per fact. The committed fixture is never edited; the map is throwaway.
''' </summary>
Public Class FixtureMapScenario
    Implements IDisposable

    ''' <summary>The temp map holding one completed run of the fixture solution.</summary>
    Public ReadOnly Property Map As TempMap

    ''' <summary>A configuration file naming the map, both gates off.</summary>
    Public ReadOnly Property ConfigPath As String

    ''' <summary>The map's solution id.</summary>
    Public ReadOnly Property SolutionId As Long

    ''' <summary>The committed fixture's Sample.sln.</summary>
    Public ReadOnly Property SolutionPath As String

    ''' <summary>An in-process host over the configuration.</summary>
    Public ReadOnly Property Host As BridgeHost

    ''' <summary>
    ''' Restores and extracts the fixture, writes the configuration.
    ''' </summary>
    Public Sub New()
        SolutionPath = Path.Combine(RepoPaths.FixtureDirectory(), "Sample.sln")
        DotnetCli.Run("restore """ & SolutionPath & """", RepoPaths.FixtureDirectory())
        Map = New TempMap()
        Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = SolutionPath, .DbPath = Map.Path}, Nothing)
        If code <> ExitCode.Success Then Throw New InvalidOperationException("fixture extraction failed: " & code.ToString())
        SolutionId = MapQueries.ReadSolutions(Map.Path)(0).Id
        ConfigPath = BridgeHost.WriteConfig(Map.Path, Nothing, False, False)
        Host = New BridgeHost(ConfigPath)
    End Sub

    ''' <summary>
    ''' The active row with a doc-comment id.
    ''' </summary>
    ''' <param name="docCommentId">The doc-comment id.</param>
    ''' <returns>The row's id; fails when there is no active row.</returns>
    Public Function SymbolId(docCommentId As String) As Long
        Dim row As SymbolRow = MapQueries.ReadSymbols(Map.Path, SolutionId).Find(Function(s As SymbolRow) s.DocCommentId = docCommentId AndAlso s.IsActive)
        If row Is Nothing Then Throw New InvalidOperationException(docCommentId & " is not an active row of the fixture map")
        Return row.Id
    End Function

    ''' <summary>
    ''' Deletes the map.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Map.Dispose()
    End Sub

End Class

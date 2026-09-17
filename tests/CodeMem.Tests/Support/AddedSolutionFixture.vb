' File: AddedSolutionFixture.vb
' Project: CodeMem.Tests
' Description: Collection fixture for B10 and B11 (feature 005, T026; analyze I3): one map seeded with Sample from one fixture copy; a second copy first refused by path (its directory observed, not in the map) and then added through the door by key with an explicit path - the one real launch, shared.
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' A collection fixture (ExtractLog), so the launch happens once per run of the collection, before its first fact; B05 and B06 share the
' collection and pay for it too - one child process, the cost of one of B05's own real launches.

Imports System.Text.Json
Imports CodeMem.Bridging
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The second copy is outside any repository, so its root is its solution file's directory (Q6); it holds Sample.sln and Sample.slnx both,
''' so the refusal before adding is AmbiguousSolutionFile - an observed directory all the same (FR-419).
''' </summary>
Public Class AddedSolutionFixture
    Implements IDisposable

    ''' <summary>The copy added as Fresh.</summary>
    Public ReadOnly Property Copy As FixtureCopy

    ''' <summary>The copy seeded as Sample, so the map has a root before the add.</summary>
    Public ReadOnly Property Seed As FixtureCopy

    ''' <summary>The map: Sample, then Fresh.</summary>
    Public ReadOnly Property Map As TempMap

    ''' <summary>A configuration over Map with both gates on and the default extractor beside the tests.</summary>
    Public ReadOnly Property ConfigPath As String

    ''' <summary>extract(repoPath = Copy.Directory) before the add: refused, the directory observed.</summary>
    Public ReadOnly Property RefusedBeforeAdding As ExtractResult

    ''' <summary>Whether map_status listed Copy.Directory under notInMap between the refusal and the add.</summary>
    Public ReadOnly Property ListedBeforeAdding As Boolean

    ''' <summary>The one real launch: solutionKey Fresh with solutionPath Copy.SolutionPath.</summary>
    Public ReadOnly Property Added As ExtractResult

    ''' <summary>
    ''' Seeds, refuses, looks, adds.
    ''' </summary>
    Public Sub New()
        Seed = New FixtureCopy()
        Copy = New FixtureCopy()
        Map = New TempMap()
        Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = Seed.SolutionPath, .DbPath = Map.Path, .SolutionKey = "Sample"}, Nothing)
        If code <> ExitCode.Success Then Throw New InvalidOperationException("seeding Sample failed: " & code.ToString())
        ConfigPath = BridgeHost.WriteConfig(Map.Path, Nothing, True, True)
        Dim door As ExtractDoor = New ExtractDoor(ConfigPath, New ProcessExtractorLauncher())
        RefusedBeforeAdding = door.Run(New ExtractRequest With {.Origin = ExtractOrigin.Tool, .RepoPath = Copy.Directory})
        ListedBeforeAdding = Listed(Copy.Directory)
        Added = door.Run(New ExtractRequest With {.Origin = ExtractOrigin.Tool, .SolutionKey = "Fresh", .SolutionPath = Copy.SolutionPath})
    End Sub

    ''' <summary>
    ''' Whether map_status over ConfigPath lists a directory under notInMap now.
    ''' </summary>
    ''' <param name="directory">The directory as a fact names it.</param>
    ''' <returns>True when an entry's path is that directory.</returns>
    Public Function Listed(directory As String) As Boolean
        Dim reply As BridgeReply = New BridgeHost(ConfigPath).Invoke("map_status", New Dictionary(Of String, Object)(StringComparer.Ordinal))
        If reply.IsError Then Throw New InvalidOperationException(reply.Text)
        Dim key As String = NormalisedDirectory(directory)
        For Each entry As JsonElement In reply.Root().GetProperty("notInMap").EnumerateArray()
            If String.Equals(entry.GetProperty("path").GetString(), key, StringComparison.OrdinalIgnoreCase) Then Return True
        Next
        Return False
    End Function

    ''' <summary>
    ''' A directory as the not-in-map list keys it: full path, platform separators, one trailing separator.
    ''' </summary>
    ''' <param name="directory">The directory as a fact names it.</param>
    ''' <returns>The key.</returns>
    Public Shared Function NormalisedDirectory(directory As String) As String
        Return SolutionScope.NormalizeDirectory(directory)
    End Function

    ''' <summary>
    ''' Deletes the map and both copies.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Map.Dispose()
        Copy.Dispose()
        Seed.Dispose()
    End Sub

End Class

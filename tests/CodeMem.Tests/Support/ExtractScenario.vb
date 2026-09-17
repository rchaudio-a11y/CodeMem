' File: ExtractScenario.vb
' Project: CodeMem.Tests
' Description: Class fixture for B05, B06 and B10: a fixture copy inside a repository extracted into two maps - Map holds Sample alone; NestedMap holds Sample, Inner (root one level down) and Twin (the same root) - fresh configs and hosts per fact (feature 004, T038; feature 005, T019).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T019): the two registries are gone. A map's roots are the roots of every solution it holds, so what registry A
' and registry B once selected over one map is now two maps: Map for the simple resolutions (Sample's root and its subdirectories, a
' directory under no root), NestedMap for the nested and ambiguous ones (Inner wins under the copy; Sample and Twin tie at the root).

Imports System.IO
Imports CodeMem.Extraction
Imports LibGit2Sharp

''' <summary>
''' The mapped root is the copy's parent directory (the repository the extractor discovers). Every host gets its own config file and
''' scripted launcher.
''' </summary>
Public Class ExtractScenario
    Implements IDisposable

    ''' <summary>The fixture copy.</summary>
    Public ReadOnly Property Copy As FixtureCopy

    ''' <summary>The mapped root: the copy's parent directory with its trailing separator, as the extractor wrote it.</summary>
    Public ReadOnly Property Root As String

    ''' <summary>Inner's root: the copy directory with its trailing separator (one level below Root).</summary>
    Public ReadOnly Property InnerRoot As String

    ''' <summary>The map holding Sample alone.</summary>
    Public ReadOnly Property Map As TempMap

    ''' <summary>The map holding Sample, Inner and Twin.</summary>
    Public ReadOnly Property NestedMap As TempMap

    ''' <summary>Map's Sample solution id.</summary>
    Public ReadOnly Property SampleId As Long

    ''' <summary>Map's first completed run's id.</summary>
    Public ReadOnly Property FirstRunId As Long

    ''' <summary>
    ''' Copies, commits, extracts Sample into Map and Sample, Inner and Twin into NestedMap.
    ''' </summary>
    Public Sub New()
        Copy = New FixtureCopy()
        Using repo As Repository = GitFixture.Init(Copy.ParentDirectory)
            GitFixture.CommitAll(repo, "c1")
        End Using
        Map = New TempMap()
        Extract(Map, "Sample")
        Dim sample As SolutionRow = MapQueries.ReadSolutions(Map.Path)(0)
        SampleId = sample.Id
        Root = sample.RepoRoot
        InnerRoot = Copy.Directory & Path.DirectorySeparatorChar
        FirstRunId = MapQueries.ReadRuns(Map.Path)(0).Id
        NestedMap = New TempMap()
        Extract(NestedMap, "Sample")
        Extract(NestedMap, "Inner")
        Extract(NestedMap, "Twin")
        Dim inner As SolutionRow = MapQueries.ReadSolutions(NestedMap.Path).Find(Function(s As SolutionRow) s.Key = "Inner")
        MapQueries.SetRepoRoot(NestedMap.Path, inner.Id, InnerRoot)
    End Sub

    ''' <summary>
    ''' A fresh config over Map with the gates as given, and a host with its own scripted launcher.
    ''' </summary>
    ''' <param name="enabled">extract.enabled.</param>
    ''' <param name="onGreenBuild">extract.onGreenBuild.</param>
    ''' <returns>The host.</returns>
    Public Function Host(enabled As Boolean, onGreenBuild As Boolean) As BridgeHost
        Return New BridgeHost(Config(enabled, onGreenBuild))
    End Function

    ''' <summary>
    ''' A fresh config over NestedMap with the gates as given, and a host with its own scripted launcher.
    ''' </summary>
    ''' <param name="enabled">extract.enabled.</param>
    ''' <param name="onGreenBuild">extract.onGreenBuild.</param>
    ''' <returns>The host.</returns>
    Public Function NestedHost(enabled As Boolean, onGreenBuild As Boolean) As BridgeHost
        Return New BridgeHost(NestedConfig(enabled, onGreenBuild))
    End Function

    ''' <summary>
    ''' A fresh config file over Map.
    ''' </summary>
    ''' <param name="enabled">extract.enabled.</param>
    ''' <param name="onGreenBuild">extract.onGreenBuild.</param>
    ''' <returns>The config path.</returns>
    Public Function Config(enabled As Boolean, onGreenBuild As Boolean) As String
        Return BridgeHost.WriteConfig(Map.Path, Nothing, enabled, onGreenBuild)
    End Function

    ''' <summary>
    ''' A fresh config file over NestedMap.
    ''' </summary>
    ''' <param name="enabled">extract.enabled.</param>
    ''' <param name="onGreenBuild">extract.onGreenBuild.</param>
    ''' <returns>The config path.</returns>
    Public Function NestedConfig(enabled As Boolean, onGreenBuild As Boolean) As String
        Return BridgeHost.WriteConfig(NestedMap.Path, Nothing, enabled, onGreenBuild)
    End Function

    ''' <summary>
    ''' A unique, non-existent directory under the mapped root: resolves to Sample and identifies one fact's log lines.
    ''' </summary>
    ''' <returns>The path.</returns>
    Public Function UniquePathUnderRoot() As String
        Return Path.Combine(Root, "probe-" & Guid.NewGuid().ToString("N"))
    End Function

    ''' <summary>
    ''' A unique, non-existent directory under no mapped root (a sibling of the repository).
    ''' </summary>
    ''' <returns>The path.</returns>
    Public Function UniquePathUnderNoRoot() As String
        Return Path.Combine(Path.GetTempPath(), "codemem-tests", "nowhere-" & Guid.NewGuid().ToString("N"))
    End Function

    ''' <summary>
    ''' Deletes the maps and the copy.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        NestedMap.Dispose()
        Map.Dispose()
        Copy.Dispose()
    End Sub

    Private Sub Extract(map As TempMap, key As String)
        Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = Copy.SolutionPath, .DbPath = map.Path, .SolutionKey = key}, Nothing)
        If code <> ExitCode.Success Then Throw New InvalidOperationException("extraction as " & key & " failed: " & code.ToString())
    End Sub

End Class

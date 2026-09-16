' File: ExtractScenario.vb
' Project: CodeMem.Tests
' Description: Class fixture for B05 and B06: a fixture copy inside a repository extracted three times (Sample, Inner with a nested root, Twin with the same root), two registries, fresh configs and hosts per fact (feature 004, T038).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO
Imports CodeMem.Extraction
Imports LibGit2Sharp

''' <summary>
''' The registered root is the copy's parent directory (the repository the extractor discovers). Registry A binds Sample and carries the
''' key-refusal rows (Unbound, Retired, Ghost); registry B binds Sample, Inner (root one level down) and Twin (the same root) for the
''' nested and ambiguous resolutions. The map is shared; every host gets its own config file and scripted launcher.
''' </summary>
Public Class ExtractScenario
    Implements IDisposable

    ''' <summary>The fixture copy.</summary>
    Public ReadOnly Property Copy As FixtureCopy

    ''' <summary>The registered root: the copy's parent directory with its trailing separator, as the extractor wrote it.</summary>
    Public ReadOnly Property Root As String

    ''' <summary>The shared map.</summary>
    Public ReadOnly Property Map As TempMap

    ''' <summary>The map's Sample solution id.</summary>
    Public ReadOnly Property SampleId As Long

    ''' <summary>Registry A: Sample bound, Unbound, Retired (inactive, 77), Ghost (99).</summary>
    Public ReadOnly Property Registry As RegistryFixture

    ''' <summary>Registry B: Sample, Inner (root one level down), Twin (the same root).</summary>
    Public ReadOnly Property NestedRegistry As RegistryFixture

    ''' <summary>The first completed run's id.</summary>
    Public ReadOnly Property FirstRunId As Long

    ''' <summary>
    ''' Copies, commits, extracts three times, seeds.
    ''' </summary>
    Public Sub New()
        Copy = New FixtureCopy()
        Using repo As Repository = GitFixture.Init(Copy.ParentDirectory)
            GitFixture.CommitAll(repo, "c1")
        End Using
        Map = New TempMap()
        Extract("Sample")
        Extract("Inner")
        Extract("Twin")
        Dim solutions As List(Of SolutionRow) = MapQueries.ReadSolutions(Map.Path)
        Dim sample As SolutionRow = solutions.Find(Function(s As SolutionRow) s.Key = "Sample")
        SampleId = sample.Id
        Root = sample.RepoRoot
        FirstRunId = MapQueries.ReadRuns(Map.Path)(0).Id
        Dim inner As SolutionRow = solutions.Find(Function(s As SolutionRow) s.Key = "Inner")
        MapQueries.SetRepoRoot(Map.Path, inner.Id, Copy.Directory & Path.DirectorySeparatorChar)
        Registry = New RegistryFixture()
        Registry.Seed(131373, "Sample", SampleId, "active", Copy.SolutionPath)
        Registry.Seed(131373, "Unbound", Nothing, "active", Copy.SolutionPath)
        Registry.Seed(131373, "Retired", 77, "inactive", Copy.SolutionPath)
        Registry.Seed(131373, "Ghost", 99, "active", Copy.SolutionPath)
        NestedRegistry = New RegistryFixture()
        NestedRegistry.Seed(131373, "Sample", SampleId, "active", Copy.SolutionPath)
        NestedRegistry.Seed(131373, "Inner", inner.Id, "active", Copy.SolutionPath)
        NestedRegistry.Seed(131373, "Twin", solutions.Find(Function(s As SolutionRow) s.Key = "Twin").Id, "active", Copy.SolutionPath)
    End Sub

    ''' <summary>
    ''' A fresh config over the shared map and registry A with the gates as given, and a host with its own scripted launcher.
    ''' </summary>
    ''' <param name="enabled">extract.enabled.</param>
    ''' <param name="onGreenBuild">extract.onGreenBuild.</param>
    ''' <returns>The host.</returns>
    Public Function Host(enabled As Boolean, onGreenBuild As Boolean) As BridgeHost
        Return New BridgeHost(Config(Registry, enabled, onGreenBuild))
    End Function

    ''' <summary>
    ''' A fresh config file over the shared map and a registry.
    ''' </summary>
    ''' <param name="registry">The registry.</param>
    ''' <param name="enabled">extract.enabled.</param>
    ''' <param name="onGreenBuild">extract.onGreenBuild.</param>
    ''' <returns>The config path.</returns>
    Public Function Config(registry As RegistryFixture, enabled As Boolean, onGreenBuild As Boolean) As String
        Return BridgeHost.WriteConfig(Map.Path, registry.Path, Nothing, enabled, onGreenBuild)
    End Function

    ''' <summary>
    ''' A unique, non-existent directory under the registered root: resolves to Sample and identifies one fact's log lines.
    ''' </summary>
    ''' <returns>The path.</returns>
    Public Function UniquePathUnderRoot() As String
        Return Path.Combine(Root, "probe-" & Guid.NewGuid().ToString("N"))
    End Function

    ''' <summary>
    ''' Deletes the registries, the map and the copy.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Registry.Dispose()
        NestedRegistry.Dispose()
        Map.Dispose()
        Copy.Dispose()
    End Sub

    Private Sub Extract(key As String)
        Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = Copy.SolutionPath, .DbPath = Map.Path, .SolutionKey = key}, Nothing)
        If code <> ExitCode.Success Then Throw New InvalidOperationException("extraction as " & key & " failed: " & code.ToString())
    End Sub

End Class

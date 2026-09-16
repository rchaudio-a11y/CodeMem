' File: MapStatusScenario.vb
' Project: CodeMem.Tests
' Description: Class fixture for B04: a fixture copy inside a repository walked through the six repository states, map_status captured at each (feature 004, T032; research R54).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO
Imports System.Text.Json
Imports CodeMem.Core
Imports CodeMem.Extraction
Imports LibGit2Sharp

''' <summary>
''' Commit c1 and extract (current); commit c2 (behind 1); modify a tracked file (dirty); discard and extract at c2; a branch from c1 with
''' commit c3 (diverged, the recorded c2 off HEAD's history); a failed run newer than the completed one (INC4); a fifth row whose root is a
''' subdirectory of the repository (no_git); the .git directory removed (no_git). Each status is a captured document.
''' </summary>
Public Class MapStatusScenario
    Implements IDisposable

    ''' <summary>The fixture copy.</summary>
    Public ReadOnly Property Copy As FixtureCopy

    ''' <summary>The temp map.</summary>
    Public ReadOnly Property Map As TempMap

    ''' <summary>The registry: Sample bound, Unbound, Retired (inactive, 77), Ghost (99), later Nested.</summary>
    Public ReadOnly Property Registry As RegistryFixture

    ''' <summary>The host over the map and the registry.</summary>
    Public ReadOnly Property Host As BridgeHost

    ''' <summary>The repository root (the copy's parent directory).</summary>
    Public ReadOnly Property Root As String

    ''' <summary>The first commit.</summary>
    Public ReadOnly Property C1 As String

    ''' <summary>The second commit.</summary>
    Public ReadOnly Property C2 As String

    ''' <summary>The commit on the side branch from c1.</summary>
    Public ReadOnly Property C3 As String

    ''' <summary>The id of the completed run at c2.</summary>
    Public ReadOnly Property Run2Id As Long

    ''' <summary>map_status at c1, clean.</summary>
    Public ReadOnly Property AtC1 As JsonDocument

    ''' <summary>map_status at c2, clean, the run still at c1.</summary>
    Public ReadOnly Property AtC2 As JsonDocument

    ''' <summary>map_status at c2 with a modified tracked file.</summary>
    Public ReadOnly Property AtC2Dirty As JsonDocument

    ''' <summary>map_status on the side branch at c3 after a run at c2, with the Nested row seeded.</summary>
    Public ReadOnly Property AtC3 As JsonDocument

    ''' <summary>map_status at c3 after a failed run newer than the completed one.</summary>
    Public ReadOnly Property AtC3AfterFailedRun As JsonDocument

    ''' <summary>map_status with the .git directory removed.</summary>
    Public ReadOnly Property WithoutGit As JsonDocument

    ''' <summary>
    ''' Walks the states.
    ''' </summary>
    Public Sub New()
        Copy = New FixtureCopy()
        Root = Copy.ParentDirectory
        Map = New TempMap()
        Registry = New RegistryFixture()
        Dim repo As Repository = GitFixture.Init(Root)
        Try
            C1 = GitFixture.CommitAll(repo, "c1")
            Extract(Nothing)
            Dim solutionId As Long = MapQueries.ReadSolutions(Map.Path)(0).Id
            Registry.Seed(1, "Sample", solutionId, "active", Copy.SolutionPath)
            Registry.Seed(2, "Unbound", Nothing, "active", Copy.SolutionPath)
            Registry.Seed(3, "Retired", 77, "inactive", Copy.SolutionPath)
            Registry.Seed(4, "Ghost", 99, "active", Copy.SolutionPath)
            Host = New BridgeHost(BridgeHost.WriteConfig(Map.Path, Registry.Path, Nothing, False, False))
            AtC1 = Status()
            GitFixture.Touch(Root, "Sample/Sample.Lib/Extra.vb", "Public Class Extra" & vbLf & "End Class" & vbLf)
            C2 = GitFixture.CommitAll(repo, "c2")
            AtC2 = Status()
            File.AppendAllText(Path.Combine(Copy.Directory, "Sample.Lib", "Consumer.vb"), "' dirty" & vbLf)
            AtC2Dirty = Status()
            GitFixture.DiscardChanges(repo)
            Extract(Nothing)
            Dim runs As List(Of RunRow) = MapQueries.ReadRuns(Map.Path)
            Run2Id = runs(runs.Count - 1).Id
            GitFixture.CheckoutNewBranch(repo, "side", C1)
            GitFixture.Touch(Root, "Sample/Sample.Lib/Side.vb", "Public Class Side" & vbLf & "End Class" & vbLf)
            C3 = GitFixture.CommitAll(repo, "c3")
            Dim nestedId As Long = ExtractAs("Nested")
            MapQueries.SetRepoRoot(Map.Path, nestedId, Path.Combine(Copy.Directory, "Sample.Lib") & Path.DirectorySeparatorChar)
            Registry.Seed(5, "Nested", nestedId, "active", Copy.SolutionPath)
            AtC3 = Status()
            Extract(New RunSeams With {.CorruptStagedCounts = Sub(c As RunCounts) c.SymbolsMatched += 1})
            AtC3AfterFailedRun = Status()
        Finally
            repo.Dispose()
        End Try
        GitFixture.RemoveGitDirectory(Root)
        WithoutGit = Status()
    End Sub

    ''' <summary>
    ''' The Sample entry of a captured status.
    ''' </summary>
    ''' <param name="status">The document.</param>
    ''' <param name="key">The registry key.</param>
    ''' <returns>The entry element.</returns>
    Public Shared Function Entry(status As JsonDocument, key As String) As JsonElement
        For Each candidate As JsonElement In status.RootElement.GetProperty("entries").EnumerateArray()
            If candidate.GetProperty("solutionKey").GetString() = key Then Return candidate
        Next
        Throw New InvalidOperationException("no entry for " & key & " in " & status.RootElement.GetRawText())
    End Function

    ''' <summary>
    ''' Deletes the registry, the map and the copy.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Registry.Dispose()
        Map.Dispose()
        Copy.Dispose()
    End Sub

    Private Function Status() As JsonDocument
        Dim reply As BridgeReply = Host.Invoke("map_status", New Dictionary(Of String, Object)())
        If reply.IsError Then Throw New InvalidOperationException("map_status refused: " & reply.Text)
        Return reply.Json
    End Function

    Private Sub Extract(seams As RunSeams)
        Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = Copy.SolutionPath, .DbPath = Map.Path, .SolutionKey = "Sample"}, seams)
        If seams Is Nothing AndAlso code <> ExitCode.Success Then Throw New InvalidOperationException("extraction failed: " & code.ToString())
        If seams IsNot Nothing AndAlso code <> ExitCode.ResidualMismatch Then Throw New InvalidOperationException("the corrupted run did not fail as expected: " & code.ToString())
    End Sub

    Private Function ExtractAs(key As String) As Long
        Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = Copy.SolutionPath, .DbPath = Map.Path, .SolutionKey = key}, Nothing)
        If code <> ExitCode.Success Then Throw New InvalidOperationException("extraction as " & key & " failed: " & code.ToString())
        Return MapQueries.ReadSolutions(Map.Path).Find(Function(s As SolutionRow) s.Key = key).Id
    End Function

End Class

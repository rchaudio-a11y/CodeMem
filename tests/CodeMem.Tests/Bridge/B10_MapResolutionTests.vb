' File: B10_MapResolutionTests.vb
' Project: CodeMem.Tests
' Description: US2 and US3 (feature 005): extract resolves a directory through the map alone; a directory under no mapped root is answered not in the map with the command that adds it; a key with an explicit path adds or re-points a solution (FR-410, FR-411, FR-414-FR-417; Q3, Q4, Q6).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Every refusal is asserted with 0 launches through the scripted launcher and the map's hash unchanged; the log lines of one fact are found
' by the unique directory it used (INC3). (10)-(16) arrive at T026 and T029.
' RED:   2026-09-17 (T019) 9 of 9 red on the first build: (1)-(4) refused where a launch was expected and (5)-(8) refused with the wrong
'        kind - every one RegistryAbsent ("The store at '?' holds no code_map_solutions table"): the door still read a store no
'        configuration names; (9) through the executable red with them. B05 15 of 18 and B06 7 of 14 red alongside (31 red / 12 green
'        of 43). GREEN at T021 - after one test-side amendment: RefusedNotInMap had asserted the directory against the wire JSON, where
'        backslashes are escaped, and never matched; it reads refusal.text from the JSON now. 5 of 9 were green before it, 9 of 9 after.
' GREEN: 2026-09-17 (T021) 9 of 9 with TargetResolver over solutions rows and SolutionFileSuggestion; B05 17 of 18 ((18) is T022's Red) and
'        B06 14 of 14 alongside.
' FIRE:  2026-09-17 (T021) (8): Inspect also listed the parent directory's .sln files -> red (Found: "Parent"); reverted from a byte copy ->
'        green. (4): ByPath skipped rows whose repo_root is NULL -> red (the Loose solution refused instead of resolving at its base
'        directory); reverted -> green. 9 of 9 after both.

Imports System.IO
Imports System.Text.Json
Imports CodeMem.Bridging
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Fast facts through the in-process door over ExtractScenario's two maps; the production route is (9) through the executable.
''' </summary>
<Collection("ExtractLog")>
Public Class B10_MapResolutionTests
    Implements IClassFixture(Of ExtractScenario)

    Private ReadOnly _scenario As ExtractScenario

    ''' <summary>
    ''' Receives the shared extraction.
    ''' </summary>
    ''' <param name="scenario">The class fixture.</param>
    Public Sub New(scenario As ExtractScenario)
        _scenario = scenario
    End Sub

    ''' <summary>
    ''' (1) The mapped root itself and a subdirectory of it resolve to the solution's key, from the map alone (FR-410).
    ''' </summary>
    <Fact>
    Public Sub ADirectoryUnderAMappedRootResolvesToItsKey()
        Dim host As BridgeHost = _scenario.Host(True, True)
        host.Launcher.Script(0, "solution=Sample run_id=" & _scenario.FirstRunId & " observed=1", "")
        For Each path As String In New String() {_scenario.Root, _scenario.UniquePathUnderRoot()}
            Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", path))
            Assert.False(reply.IsError, path & ": " & reply.Text)
            Assert.Equal("Sample", reply.Root().GetProperty("target").GetProperty("resolvedKey").GetString())
        Next
        Assert.Equal(2, host.Launcher.Requests.Count)
        Assert.Equal("Sample", host.Launcher.Requests(0).SolutionKey)
    End Sub

    ''' <summary>
    ''' (2) Two mapped roots nest: a directory under the inner one resolves to Inner, the longest root (FR-410).
    ''' </summary>
    <Fact>
    Public Sub NestedRootsResolveToTheLongest()
        Dim host As BridgeHost = _scenario.NestedHost(True, True)
        host.Launcher.Script(0, "solution=Inner run_id=1 observed=1", "")
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", Path.Combine(_scenario.InnerRoot, "Sample.Lib")))
        Assert.False(reply.IsError, reply.Text)
        Assert.Equal("Inner", reply.Root().GetProperty("target").GetProperty("resolvedKey").GetString())
    End Sub

    ''' <summary>
    ''' (3) Two map solutions share one root: a directory under it and outside the inner root is ambiguous, naming both keys, 0 launches.
    ''' </summary>
    <Fact>
    Public Sub ASharedRootIsAmbiguous()
        Dim host As BridgeHost = _scenario.NestedHost(True, True)
        Dim hash As String = MapSnapshot.FileBytesHash(_scenario.NestedMap.Path)
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", _scenario.UniquePathUnderRoot()))
        Assert.True(reply.IsError, "not refused: " & reply.Text)
        Assert.Contains("more than one mapped solution", reply.Text, StringComparison.Ordinal)
        Assert.Contains("Sample", reply.Text, StringComparison.Ordinal)
        Assert.Contains("Twin", reply.Text, StringComparison.Ordinal)
        Assert.Equal(0, host.Launcher.Requests.Count)
        Assert.Equal(hash, MapSnapshot.FileBytesHash(_scenario.NestedMap.Path))
    End Sub

    ''' <summary>
    ''' (4) A solution extracted outside any repository has no repository root: a directory under its solution file's directory resolves
    ''' to it all the same - the extractor's own root rule (Q6 as ruled).
    ''' </summary>
    <Fact>
    Public Sub ASolutionWithoutARepositoryRootResolvesAtItsBaseDirectory()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path, .SolutionKey = "Loose"}, Nothing))
                Assert.Null(MapQueries.ReadSolutions(map.Path)(0).RepoRoot)
                Dim host As BridgeHost = New BridgeHost(BridgeHost.WriteConfig(map.Path, Nothing, True, True))
                host.Launcher.Script(0, "solution=Loose run_id=1 observed=1", "")
                Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", Path.Combine(copy.Directory, "Sample.Lib")))
                Assert.False(reply.IsError, reply.Text)
                Assert.Equal("Loose", reply.Root().GetProperty("target").GetProperty("resolvedKey").GetString())
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (5) A directory under no mapped root holding exactly one .slnx: not in the map, the text carrying the exact command that adds it,
    ''' 0 launches, the map byte-identical (FR-411).
    ''' </summary>
    <Fact>
    Public Sub AnUnmappedDirectoryWithOneSlnxIsNotInMapWithTheCommand()
        Dim dir As String = _scenario.UniquePathUnderNoRoot()
        Directory.CreateDirectory(dir)
        Dim file As String = Path.Combine(dir, "Fresh.slnx")
        IO.File.WriteAllText(file, "<Solution />")
        Dim text As String = RefusedNotInMap(dir)
        Assert.Contains("extract --solution-key Fresh --solution " & file, text, StringComparison.Ordinal)
        Assert.DoesNotContain("<key>", text, StringComparison.Ordinal)
    End Sub

    ''' <summary>
    ''' (6) A directory under no mapped root holding no solution file: not in the map, the command with placeholders.
    ''' </summary>
    <Fact>
    Public Sub AnUnmappedDirectoryWithNoSolutionFileNamesPlaceholders()
        Dim dir As String = _scenario.UniquePathUnderNoRoot()
        Directory.CreateDirectory(dir)
        Dim text As String = RefusedNotInMap(dir)
        Assert.Contains("no solution file", text, StringComparison.Ordinal)
        Assert.Contains("--solution-key <key> --solution <path", text, StringComparison.Ordinal)
    End Sub

    ''' <summary>
    ''' (7) A directory under no mapped root holding two solution files (A.sln and A.slnx): ambiguous, both named, no key suggested.
    ''' </summary>
    <Fact>
    Public Sub AnUnmappedDirectoryWithTwoFilesIsAmbiguous()
        Dim dir As String = _scenario.UniquePathUnderNoRoot()
        Directory.CreateDirectory(dir)
        IO.File.WriteAllText(Path.Combine(dir, "A.sln"), "")
        IO.File.WriteAllText(Path.Combine(dir, "A.slnx"), "<Solution />")
        Dim host As BridgeHost = _scenario.Host(True, True)
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", dir))
        Assert.True(reply.IsError, "not refused: " & reply.Text)
        Assert.Contains("more than one solution file", reply.Text, StringComparison.Ordinal)
        Assert.Contains("A.sln", reply.Text, StringComparison.Ordinal)
        Assert.Contains("A.slnx", reply.Text, StringComparison.Ordinal)
        Assert.DoesNotContain("--solution-key A ", reply.Text, StringComparison.Ordinal)
        Assert.Equal(0, host.Launcher.Requests.Count)
    End Sub

    ''' <summary>
    ''' (8) The suggestion inspects the given directory only: a child directory of an unmapped tree whose parent holds Parent.sln is answered
    ''' with placeholders, never with Parent (Q4).
    ''' </summary>
    <Fact>
    Public Sub TheSuggestionNeverLooksUpOrDown()
        Dim parent As String = _scenario.UniquePathUnderNoRoot()
        Dim child As String = Path.Combine(parent, "child")
        Directory.CreateDirectory(child)
        IO.File.WriteAllText(Path.Combine(parent, "Parent.sln"), "")
        Dim text As String = RefusedNotInMap(child)
        Assert.DoesNotContain("Parent", text, StringComparison.Ordinal)
        Assert.Contains("no solution file", text, StringComparison.Ordinal)
        Dim grandparentOfAFile As String = _scenario.UniquePathUnderNoRoot()
        Directory.CreateDirectory(Path.Combine(grandparentOfAFile, "sub"))
        IO.File.WriteAllText(Path.Combine(grandparentOfAFile, "sub", "Below.sln"), "")
        Dim above As String = RefusedNotInMap(grandparentOfAFile)
        Assert.DoesNotContain("Below", above, StringComparison.Ordinal)
    End Sub

    ''' <summary>
    ''' (9) The not-in-map answer reaches the command line: the extract entry exits 2 with the kind and the command in its JSON (CON3).
    ''' </summary>
    <Fact>
    Public Sub TheNotInMapAnswerReachesTheCommandLine()
        Dim dir As String = _scenario.UniquePathUnderNoRoot()
        Directory.CreateDirectory(dir)
        Dim file As String = Path.Combine(dir, "Fresh.slnx")
        IO.File.WriteAllText(file, "<Solution />")
        Dim reply As ProcessReply = BridgeProcess.RunOnce("extract --repo-path " & BridgeProcess.Quote(dir) & " --config " & BridgeProcess.Quote(_scenario.Config(True, True)), "")
        Assert.Equal(2, reply.ExitCode)
        Using document As JsonDocument = JsonDocument.Parse(reply.StandardOutput)
            Dim refusal As JsonElement = document.RootElement.GetProperty("refusal")
            Assert.Equal("PathNotInMap", refusal.GetProperty("kind").GetString())
            Assert.Contains("extract --solution-key Fresh --solution", refusal.GetProperty("text").GetString(), StringComparison.Ordinal)
            Assert.False(document.RootElement.GetProperty("launched").GetBoolean())
        End Using
    End Sub

    ''' <summary>
    ''' The refusal text of a PathNotInMap answer for dir, with the invariants every such answer keeps: refused, the phrase, the directory
    ''' named, 0 launches, the map byte-identical, one log line. The text is read from the JSON, not the wire (backslashes are escaped there).
    ''' </summary>
    Private Function RefusedNotInMap(dir As String) As String
        Dim host As BridgeHost = _scenario.Host(True, True)
        Dim hash As String = MapSnapshot.FileBytesHash(_scenario.Map.Path)
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", dir))
        Assert.True(reply.IsError, "not refused: " & reply.Text)
        Dim refusal As JsonElement = reply.Root().GetProperty("refusal")
        Assert.Equal("PathNotInMap", refusal.GetProperty("kind").GetString())
        Dim text As String = refusal.GetProperty("text").GetString()
        Assert.Contains("not in the map", text, StringComparison.Ordinal)
        Assert.Contains(dir, text, StringComparison.Ordinal)
        Assert.Equal(0, host.Launcher.Requests.Count)
        Assert.Equal(hash, MapSnapshot.FileBytesHash(_scenario.Map.Path))
        Dim lines As List(Of String()) = LogLines.Containing(dir)
        Assert.Single(lines)
        Return text
    End Function

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

End Class

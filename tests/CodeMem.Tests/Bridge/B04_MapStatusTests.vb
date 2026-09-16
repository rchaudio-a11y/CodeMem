' File: B04_MapStatusTests.vb
' Project: CodeMem.Tests
' Description: map_status on a repository fixture: the six verdicts in the ruled order, nothing guessed, unbound and inactive rows listed, the latest completed run compared, the description (FR-320..FR-324, FR-345, spec Q4, INC4).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' RED:   2026-09-15 (T032) all ten red: the class fixture's first map_status call threw NotImplementedException("US3") from
'        BridgeTools.MapStatus - the named Red (the scenario's extractions ran first).
' GREEN: 2026-09-15 (T034) 10 of 10 after RepositoryFacts, MapStatusReader, the six envelopes and the registration (T033); B01 (4) over
'        seven tools.
' FIRE:  2026-09-15 (T034) behind evaluated before dirty in MapStatusReader.EntryOf -> (3) red (verdict "behind" where "dirty" was asserted);
'        restored from a byte copy -> green.

Imports System.IO
Imports System.Text.Json
Imports CodeMem.Bridging
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The states are walked once by MapStatusScenario; the facts read the captured documents.
''' </summary>
<Collection("Fixture")>
Public Class B04_MapStatusTests
    Implements IClassFixture(Of MapStatusScenario)

    Private ReadOnly _scenario As MapStatusScenario

    ''' <summary>
    ''' Receives the walked scenario.
    ''' </summary>
    ''' <param name="scenario">The class fixture.</param>
    Public Sub New(scenario As MapStatusScenario)
        _scenario = scenario
    End Sub

    ''' <summary>
    ''' (1) HEAD equal to the run's commit and a clean tree: current, behindBy 0, treeDirty false, every property present.
    ''' </summary>
    <Fact>
    Public Sub CurrentWhenHeadEqualsTheRunAndTheTreeIsClean()
        Dim entry As JsonElement = MapStatusScenario.Entry(_scenario.AtC1, "Sample")
        AssertHas(entry, "solutionKey", "projectId", "codememSolutionId", "run", "repoRoot", "head", "verdict", "reason")
        AssertHas(entry.GetProperty("head"), "sha", "treeDirty", "behindBy", "note")
        AssertHas(entry.GetProperty("run"), "runId", "commitSha", "isDirty", "finishedUtc")
        Assert.Equal("current", entry.GetProperty("verdict").GetString())
        Assert.Equal(_scenario.C1, entry.GetProperty("head").GetProperty("sha").GetString())
        Assert.Equal(0, entry.GetProperty("head").GetProperty("behindBy").GetInt32())
        Assert.False(entry.GetProperty("head").GetProperty("treeDirty").GetBoolean())
        Assert.Equal(_scenario.C1, entry.GetProperty("run").GetProperty("commitSha").GetString())
    End Sub

    ''' <summary>
    ''' (2) One commit past the run: behind, behindBy 1, HEAD named, the reason naming both short shas.
    ''' </summary>
    <Fact>
    Public Sub BehindCountsCommitsAndNamesHead()
        Dim entry As JsonElement = MapStatusScenario.Entry(_scenario.AtC2, "Sample")
        Assert.Equal("behind", entry.GetProperty("verdict").GetString())
        Assert.Equal(1, entry.GetProperty("head").GetProperty("behindBy").GetInt32())
        Assert.Equal(_scenario.C2, entry.GetProperty("head").GetProperty("sha").GetString())
        Dim reason As String = entry.GetProperty("reason").GetString()
        Assert.Contains(_scenario.C1.Substring(0, 8), reason)
        Assert.Contains(_scenario.C2.Substring(0, 8), reason)
    End Sub

    ''' <summary>
    ''' (3) An uncommitted edit on top of (2): dirty wins, the count still reported beside it.
    ''' </summary>
    <Fact>
    Public Sub DirtyWinsOverBehind()
        Dim entry As JsonElement = MapStatusScenario.Entry(_scenario.AtC2Dirty, "Sample")
        Assert.Equal("dirty", entry.GetProperty("verdict").GetString())
        Assert.True(entry.GetProperty("head").GetProperty("treeDirty").GetBoolean())
        Assert.Equal(1, entry.GetProperty("head").GetProperty("behindBy").GetInt32())
    End Sub

    ''' <summary>
    ''' (4) A branch from c1 with its own commit after a run at c2: diverged, behindBy null, the reason naming c2 and c3.
    ''' </summary>
    <Fact>
    Public Sub DivergedNamesBothShasAndClaimsNoCount()
        Dim entry As JsonElement = MapStatusScenario.Entry(_scenario.AtC3, "Sample")
        Assert.Equal("diverged", entry.GetProperty("verdict").GetString())
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("head").GetProperty("behindBy").ValueKind)
        Assert.Equal(_scenario.C3, entry.GetProperty("head").GetProperty("sha").GetString())
        Dim reason As String = entry.GetProperty("reason").GetString()
        Assert.Contains(_scenario.C2.Substring(0, 8), reason)
        Assert.Contains(_scenario.C3.Substring(0, 8), reason)
    End Sub

    ''' <summary>
    ''' (5) The .git directory removed: no_git with the reason; a registered root that is a subdirectory of a repository: no_git, not a
    ''' repository working directory, and the other entries unaffected.
    ''' </summary>
    <Fact>
    Public Sub NoGitWhenTheRootIsNotARepository()
        Dim gone As JsonElement = MapStatusScenario.Entry(_scenario.WithoutGit, "Sample")
        Assert.Equal("no_git", gone.GetProperty("verdict").GetString())
        Assert.Contains("no repository", gone.GetProperty("head").GetProperty("note").GetString())
        Assert.Equal(JsonValueKind.Null, gone.GetProperty("head").GetProperty("sha").ValueKind)
        Dim nested As JsonElement = MapStatusScenario.Entry(_scenario.AtC3, "Nested")
        Assert.Equal("no_git", nested.GetProperty("verdict").GetString())
        Assert.Contains("not a repository working directory", nested.GetProperty("head").GetProperty("note").GetString())
        Assert.Equal("diverged", MapStatusScenario.Entry(_scenario.AtC3, "Sample").GetProperty("verdict").GetString())
    End Sub

    ''' <summary>
    ''' (6) A row bound to an id the map lacks is map_missing_solution naming key and id; unbound and inactive rows are listed with no
    ''' verdict; bound counts the active bound rows.
    ''' </summary>
    <Fact>
    Public Sub MapMissingSolutionUnboundAndInactiveAreListed()
        Dim root As JsonElement = _scenario.AtC1.RootElement
        Dim ghost As JsonElement = MapStatusScenario.Entry(_scenario.AtC1, "Ghost")
        Assert.Equal("map_missing_solution", ghost.GetProperty("verdict").GetString())
        Assert.Contains("Ghost", ghost.GetProperty("reason").GetString())
        Assert.Contains("99", ghost.GetProperty("reason").GetString())
        Assert.Equal(JsonValueKind.Null, ghost.GetProperty("run").ValueKind)
        Assert.Equal(2, root.GetProperty("bound").GetInt32())
        Assert.Equal(1, root.GetProperty("unbound").GetArrayLength())
        Assert.Equal("Unbound", root.GetProperty("unbound")(0).GetProperty("solutionKey").GetString())
        Assert.Equal(1, root.GetProperty("inactive").GetArrayLength())
        Assert.Equal("Retired", root.GetProperty("inactive")(0).GetProperty("solutionKey").GetString())
        Assert.Equal("inactive", root.GetProperty("inactive")(0).GetProperty("state").GetString())
    End Sub

    ''' <summary>
    ''' (7) A map extracted from a copy outside any repository (no .git above it): no_git with the reason "no commit recorded".
    ''' </summary>
    <Fact>
    Public Sub NoCommitRecordedIsNoGit()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Using registry As RegistryFixture = New RegistryFixture()
                    Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path, .SolutionKey = "Sample"}, Nothing))
                    Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
                    Assert.Null(runs(runs.Count - 1).CommitSha)
                    registry.Seed(1, "Sample", MapQueries.ReadSolutions(map.Path)(0).Id, "active", copy.SolutionPath)
                    Dim reply As BridgeReply = New BridgeHost(BridgeHost.WriteConfig(map.Path, registry.Path, Nothing, False, False)).Invoke("map_status", New Dictionary(Of String, Object)())
                    Assert.False(reply.IsError, reply.Text)
                    Dim entry As JsonElement = MapStatusScenario.Entry(reply.Json, "Sample")
                    Assert.Equal("no_git", entry.GetProperty("verdict").GetString())
                    Assert.Contains("no commit recorded", entry.GetProperty("head").GetProperty("note").GetString())
                End Using
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (7b) A failed run newer than the completed one: the entry's run is the completed one and the verdict is computed from it (INC4).
    ''' </summary>
    <Fact>
    Public Sub TheLatestCompletedRunIsCompared()
        Dim entry As JsonElement = MapStatusScenario.Entry(_scenario.AtC3AfterFailedRun, "Sample")
        Assert.Equal(_scenario.Run2Id, entry.GetProperty("run").GetProperty("runId").GetInt64())
        Assert.Equal("diverged", entry.GetProperty("verdict").GetString())
        Dim runs As List(Of RunRow) = MapQueries.ReadRuns(_scenario.Map.Path)
        Assert.Equal("failed", runs(runs.Count - 1).Outcome)
        Assert.True(runs(runs.Count - 1).Id > _scenario.Run2Id, "the failed run is not the newest")
    End Sub

    ''' <summary>
    ''' (8) The registered description names the six verdicts and says one verdict by name.
    ''' </summary>
    <Fact>
    Public Sub TheDescriptionNamesTheSixVerdicts()
        Dim description As String = BridgeTools.RegisteredToolDescriptions("map_status")
        For Each phrase As String In New String() {"current", "behind", "dirty", "no_git", "map_missing_solution", "diverged", "one verdict by name"}
            Assert.Contains(phrase, description)
        Next
    End Sub

    ''' <summary>
    ''' (9) An empty registry is a state, not a refusal: no entries, bound 0, unbound and inactive empty.
    ''' </summary>
    <Fact>
    Public Sub AnEmptyRegistryIsAStateNotARefusal()
        Using registry As RegistryFixture = New RegistryFixture()
            Dim reply As BridgeReply = New BridgeHost(BridgeHost.WriteConfig(_scenario.Map.Path, registry.Path, Nothing, False, False)).Invoke("map_status", New Dictionary(Of String, Object)())
            Assert.False(reply.IsError, reply.Text)
            Dim root As JsonElement = reply.Root()
            Assert.Equal(0, root.GetProperty("entries").GetArrayLength())
            Assert.Equal(0, root.GetProperty("bound").GetInt32())
            Assert.Equal(0, root.GetProperty("unbound").GetArrayLength())
            Assert.Equal(0, root.GetProperty("inactive").GetArrayLength())
        End Using
    End Sub

    Private Shared Sub AssertHas(element As JsonElement, ParamArray names As String())
        Dim value As JsonElement
        For Each name As String In names
            Assert.True(element.TryGetProperty(name, value), "property missing: " & name & " in " & element.GetRawText())
        Next
    End Sub

End Class

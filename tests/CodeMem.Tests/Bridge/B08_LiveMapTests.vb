' File: B08_LiveMapTests.vb
' Project: CodeMem.Tests
' Description: Skip-armed facts on the live map and store (read-only): the three solutions, 003's figures, the registry route, each call under 3 s, the map's hash unchanged (SC-301..SC-304, SC-309; FR-344, FR-345; research R54).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' Armed by CODEMEM_LIVE_MAP and CODEMEM_LIVE_STORE (paths); unset -> Skipped, as AcceptanceRunner is armed by CODEMEM_ACCEPT_SOLUTION.
' Every fact hashes the live map before and after its calls. The live figures are recorded in the quickstart's Record table; a number
' that differs from 003's record is a Red to diagnose, and only the Architect accepts a changed number.
' RED:   2026-09-15 (T025) (3) red on the first armed run: total 299, expected 279. Diagnosed on the live map, read-only (the scratch
'        orphans-diag query, the bridge's own statement): the 20 extra rows are the 19 properties and 1 field that 060 FR-405 made examined
'        (003's 279 was recorded while namespace, project, field and property were unexamined); over the kinds 003 examined the bridge
'        reports exactly 279 = 73 + 206. The fact asserts both figures. Ruled 2026-09-16 (Architect): 299 is the accepted live baseline;
'        the next change to that number is a Red to diagnose.
'        (1), (2), (4) green: solutions 7 ms, references 4 ms, symbol_search 15 ms, orphans 79 ms; the live map's hash unchanged.
' RED:   2026-09-16 (T051) (7) red on its first armed run: total 2 where T051's text said 1 - the name filter is a contains match and
'        also finds CodeMemMapFixtureTests (id 3111). The fact now asserts total 2 and picks the declaration named CodeMemMapFixture.
' GREEN: 2026-09-16 (T051) (7): 3556 and 4200 presented as one declaration; 14713 active MemOS rows present as 14491 declarations, 222
'        absorbed - every one of the 222 twin groups is a pair (read-only diagnosis on the live map); the twin search 63 ms, the
'        fourteen per-kind searches 4-52 ms; the live map's hash unchanged.
' RED:   2026-09-16 (T054) the armed whole-suite run: (6) red - map_status took 4108 ms under the parallel load of the extraction
'        scenarios (154 ms alone, T035). The class now sits in the LiveMap collection (DisableParallelization), so SC-301 measures the
'        bridge, not the suite.
' RED:   2026-09-16 (T060) after run 7 published on the live map (the Architect's request, backup taken first): (5), (6), (7) red -
'        references(3023) 1 where 0 was asserted, MemOS "current" where behind/dirty was, presented 15515 where 15570 was. Each diagnosed
'        read-only against the map and the answers: a test added after run 6 names CodeMemMapReader once (uses, never constructed;
'        type_usages 25 against references 1); MemOS is at the run's commit; the eleven commits added 55 twin pairs (277, all pairs).
'        Each fact re-pinned to the live state with the diagnosis in its comment (the rule: a changed live number is a Red to diagnose).

Imports System.Diagnostics
Imports System.Linq
Imports System.Text.Json
Imports CodeMem.Bridging
Imports Xunit
Imports Xunit.Abstractions

''' <summary>
''' The live evidence, never gated in the unarmed suite. In-process through BridgeHost with both gates off: nothing here can launch anything.
''' </summary>
<Collection("LiveMap")>
Public Class B08_LiveMapTests

    Private Const Budget As Integer = 3000

    Private ReadOnly _output As ITestOutputHelper

    ''' <summary>
    ''' Receives the test output sink.
    ''' </summary>
    ''' <param name="output">Where the timings are printed.</param>
    Public Sub New(output As ITestOutputHelper)
        _output = output
    End Sub

    ''' <summary>
    ''' (1) solutions lists GameRoom, CodeMem and MemOS with runs 4, 5 and 6 or later.
    ''' </summary>
    <SkippableFact>
    Public Sub LiveSolutionsListsThree()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "solutions", Args())
        Dim runs As Dictionary(Of String, Long) = New Dictionary(Of String, Long)(StringComparer.Ordinal)
        For Each solution As JsonElement In reply.Root().GetProperty("solutions").EnumerateArray()
            runs(solution.GetProperty("key").GetString()) = solution.GetProperty("latestRun").GetProperty("runId").GetInt64()
        Next
        Assert.True(runs("GameRoom") >= 4, "GameRoom run " & runs("GameRoom"))
        Assert.True(runs("CodeMem") >= 5, "CodeMem run " & runs("CodeMem"))
        Assert.True(runs("MemOS") >= 6, "MemOS run " & runs("MemOS"))
        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' (2) references on GameRoom symbol 1200 (Shoe.Draw): 7, all calls, all in BlackjackControl.vb (058's live record).
    ''' </summary>
    <SkippableFact>
    Public Sub LiveReferencesOnShoeDrawAreSeven()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "references", Args("solutionKey", "GameRoom", "symbolId", 1200L))
        Assert.Equal(7, reply.Root().GetProperty("count").GetInt32())
        For Each occurrence As JsonElement In reply.Root().GetProperty("occurrences").EnumerateArray()
            Assert.Equal("calls", occurrence.GetProperty("verb").GetString())
            Assert.Equal("GameRoom/Games/Blackjack/BlackjackControl.vb", occurrence.GetProperty("path").GetString())
        Next
        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' (3) orphans on GameRoom: over the kinds 003 examined, exactly 279 with 73 in GameRoom and 206 in GameRoom.Tests (003's figures);
    ''' plus the 20 field and property rows 060 FR-405 made examined, 299 in total (the changed total recorded for the Architect, T061).
    ''' </summary>
    <SkippableFact>
    Public Sub LiveOrphansGameRoomIsTwoHundredSeventyNine()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "orphans", Args("solutionKey", "GameRoom"))
        Dim root As JsonElement = reply.Root()
        Dim legacy As Integer = 0
        Dim fieldOrProperty As Integer = 0
        Dim legacyByProject As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)(StringComparer.Ordinal)
        For Each orphan As JsonElement In root.GetProperty("orphans").EnumerateArray()
            Dim kind As String = orphan.GetProperty("kind").GetString()
            If kind = "field" OrElse kind = "property" Then
                fieldOrProperty += 1
            Else
                legacy += 1
                Dim project As String = orphan.GetProperty("project").GetProperty("name").GetString()
                legacyByProject(project) = If(legacyByProject.ContainsKey(project), legacyByProject(project), 0) + 1
            End If
        Next
        _output.WriteLine("orphans over 003's kinds " & legacy & " (GameRoom " & legacyByProject("GameRoom") & ", GameRoom.Tests " & legacyByProject("GameRoom.Tests") & "); field/property rows " & fieldOrProperty & "; total " & root.GetProperty("total").GetInt32())
        Assert.Equal(279, legacy)
        Assert.Equal(73, legacyByProject("GameRoom"))
        Assert.Equal(206, legacyByProject("GameRoom.Tests"))
        Assert.Equal(20, fieldOrProperty)
        Assert.Equal(299, root.GetProperty("total").GetInt32())
        Assert.Equal(root.GetProperty("orphans").GetArrayLength(), root.GetProperty("total").GetInt32())
        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' (4) symbol_search by projectId 132040 resolves through the registry to GameRoom: btnDeal_Click is one row, id 584.
    ''' </summary>
    <SkippableFact>
    Public Sub LiveSearchByProjectResolvesThroughTheRegistry()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "symbol_search", Args("projectId", 132040L, "name", "btnDeal_Click"))
        Assert.Equal("projectId", reply.Root().GetProperty("scope").GetProperty("by").GetString())
        Assert.Equal(1, reply.Root().GetProperty("symbols").GetArrayLength())
        Assert.Equal(584L, reply.Root().GetProperty("symbols")(0).GetProperty("id").GetInt64())
        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' (5) type_usages on MemOS symbol 3023 (CodeMemMapReader): at least 16 constructor calls naming 6661, total at least 20, fromOutside
    ''' at least 16, no edgeId repeated; references(3023) exactly 1 since run 7 - one uses occurrence in
    ''' CodeMemRepoRootMatcherTests.vb, a test added to the tree after run 6 that names the type and never constructs it (SC-303, FR-344,
    ''' DUP1: type_usages 25 against references 1 is finding 152647 on the live map).
    ''' </summary>
    <SkippableFact>
    Public Sub LiveTypeUsagesOnCodeMemMapReader()
        Dim live As LiveBridge = Arm()
        Dim usages As BridgeReply = Timed(live, "type_usages", Args("solutionKey", "MemOS", "symbolId", 3023L))
        Dim root As JsonElement = usages.Root()
        Dim constructorCalls As Integer = 0
        Dim seen As HashSet(Of Long) = New HashSet(Of Long)()
        For Each occurrence As JsonElement In root.GetProperty("occurrences").EnumerateArray()
            Assert.True(seen.Add(occurrence.GetProperty("edgeId").GetInt64()), "edge " & occurrence.GetProperty("edgeId").GetInt64() & " repeated")
            Dim target As JsonElement = occurrence.GetProperty("target")
            If target.GetProperty("role").GetString() = "constructor" AndAlso target.GetProperty("id").GetInt64() = 6661L AndAlso occurrence.GetProperty("verb").GetString() = "calls" Then constructorCalls += 1
        Next
        _output.WriteLine("type_usages(3023): constructor calls " & constructorCalls & ", total " & root.GetProperty("total").GetInt32() & ", fromOutside " & root.GetProperty("fromOutside").GetInt32())
        Assert.True(constructorCalls >= 16, "constructor calls " & constructorCalls)
        Assert.True(root.GetProperty("total").GetInt32() >= 20, "total " & root.GetProperty("total").GetInt32())
        Assert.True(root.GetProperty("fromOutside").GetInt32() >= 16, "fromOutside " & root.GetProperty("fromOutside").GetInt32())
        Dim references As BridgeReply = Timed(live, "references", Args("solutionKey", "MemOS", "symbolId", 3023L))
        Assert.Equal(1, references.Root().GetProperty("count").GetInt32())
        Dim only As JsonElement = Assert.Single(references.Root().GetProperty("occurrences").EnumerateArray().ToList())
        Assert.Equal("uses", only.GetProperty("verb").GetString())
        Assert.EndsWith("CodeMemRepoRootMatcherTests.vb", only.GetProperty("path").GetString())
        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' (6) map_status on the live registry after run 7 (T060): the MemOS entry current, HEAD equal to the run's commit, behindBy 0; the
    ''' GameRoom and CodeMem entries present with non-null heads; unbound empty (SC-304, FR-345). Before run 7 it asserted behind or dirty
    ''' with HEAD past the recorded commit - the state finding 152646 named; the next MemOS commit turns this red, to diagnose.
    ''' </summary>
    <SkippableFact>
    Public Sub LiveMapStatusReportsMemOsBehind()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "map_status", Args())
        Dim root As JsonElement = reply.Root()
        Dim entries As Dictionary(Of String, JsonElement) = New Dictionary(Of String, JsonElement)(StringComparer.Ordinal)
        For Each entry As JsonElement In root.GetProperty("entries").EnumerateArray()
            entries(entry.GetProperty("solutionKey").GetString()) = entry
            _output.WriteLine(entry.GetProperty("solutionKey").GetString() & ": " & entry.GetProperty("verdict").GetString() & " - " & entry.GetProperty("reason").GetString())
        Next
        Dim memos As JsonElement = entries("MemOS")
        Assert.Equal("current", memos.GetProperty("verdict").GetString())
        Assert.Equal(JsonValueKind.String, memos.GetProperty("head").GetProperty("sha").ValueKind)
        Assert.Equal(memos.GetProperty("run").GetProperty("commitSha").GetString(), memos.GetProperty("head").GetProperty("sha").GetString())
        Assert.Equal(0, memos.GetProperty("head").GetProperty("behindBy").GetInt32())
        For Each key As String In New String() {"GameRoom", "CodeMem"}
            Assert.Equal(JsonValueKind.String, entries(key).GetProperty("head").GetProperty("sha").ValueKind)
        Next
        Assert.Equal(0, root.GetProperty("unbound").GetArrayLength())
        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' (7) Twins on the live MemOS map (SC-309): CodeMemMapFixture presents once with compiledInto 3556 and 4200 (total 2: the contains
    ''' filter also finds CodeMemMapFixtureTests); the declarations presented across every kind number the active rows minus 277 (222
    ''' before run 7; the eleven commits since run 6 added 55 twin pairs, every group still a pair - read-only diagnosis, T060); every
    ''' call under 3 s, timed and printed (SC-301, G3).
    ''' </summary>
    <SkippableFact>
    Public Sub LiveTwinsFoldOnMemOs()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "symbol_search", Args("solutionKey", "MemOS", "name", "CodeMemMapFixture", "kind", "class"))
        Dim root As JsonElement = reply.Root()
        Assert.Equal(2, root.GetProperty("total").GetInt32())
        Dim named As List(Of JsonElement) = New List(Of JsonElement)()
        For Each symbol As JsonElement In root.GetProperty("symbols").EnumerateArray()
            If symbol.GetProperty("name").GetString() = "CodeMemMapFixture" Then named.Add(symbol)
        Next
        Dim fixture As JsonElement = Assert.Single(named)
        Dim ids As List(Of Long) = New List(Of Long)()
        For Each twin As JsonElement In fixture.GetProperty("compiledInto").EnumerateArray()
            ids.Add(twin.GetProperty("symbolId").GetInt64())
        Next
        Assert.Equal(New Long() {3556, 4200}, ids.ToArray())
        Dim presented As Integer = 0
        For Each kind As String In SymbolKinds.All
            presented += Timed(live, "symbol_search", Args("solutionKey", "MemOS", "kind", kind)).Root().GetProperty("total").GetInt32()
        Next
        Dim active As Integer = live.ActiveSymbolCount("MemOS")
        _output.WriteLine("MemOS active rows " & active & ", presented " & presented & ", absorbed " & (active - presented))
        Assert.Equal(active - 277, presented)
        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' Arms the facts: both variables set, else Skipped.
    ''' </summary>
    ''' <returns>The host over the live paths with the hash taken.</returns>
    Private Shared Function Arm() As LiveBridge
        Dim mapPath As String = Environment.GetEnvironmentVariable("CODEMEM_LIVE_MAP")
        Dim storePath As String = Environment.GetEnvironmentVariable("CODEMEM_LIVE_STORE")
        Skip.If(String.IsNullOrEmpty(mapPath) OrElse String.IsNullOrEmpty(storePath), "CODEMEM_LIVE_MAP and CODEMEM_LIVE_STORE are not both set")
        Return New LiveBridge(mapPath, storePath)
    End Function

    ''' <summary>
    ''' One call, timed against the 3 s budget (SC-301), the time printed, the reply asserted not an error.
    ''' </summary>
    ''' <param name="live">The armed host.</param>
    ''' <param name="tool">The tool name.</param>
    ''' <param name="args">The arguments.</param>
    ''' <returns>The reply.</returns>
    Private Function Timed(live As LiveBridge, tool As String, args As Dictionary(Of String, Object)) As BridgeReply
        Dim watch As Stopwatch = Stopwatch.StartNew()
        Dim reply As BridgeReply = live.Host.Invoke(tool, args)
        watch.Stop()
        _output.WriteLine(tool & " " & watch.ElapsedMilliseconds & " ms")
        Assert.False(reply.IsError, tool & ": " & reply.Text)
        Assert.True(watch.ElapsedMilliseconds < Budget, tool & " took " & watch.ElapsedMilliseconds & " ms")
        Return reply
    End Function

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

End Class

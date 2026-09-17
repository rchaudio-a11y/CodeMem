' File: B08_LiveMapTests.vb
' Project: CodeMem.Tests
' Description: Skip-armed facts on the live map (read-only): the five solutions, 003's figures, resolution from the map alone, each call under 3 s, the map's hash unchanged (SC-301..SC-304, SC-309; FR-344, FR-345; 005 FR-434; research R54).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' Armed by CODEMEM_LIVE_MAP (a path); unset -> Skipped, as AcceptanceRunner is armed by CODEMEM_ACCEPT_SOLUTION.
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
' 2026-09-17 (feature 005, T023): CODEMEM_LIVE_STORE is gone with the store. (4) by projectId retired - its route left the bridge; the
'        004 body is in _Archive/004-store/tests/B08_RetiredFacts.vb - and asked again by solutionKey GameRoom (the same row, id 584).
'        (1) expects five solutions (DSP_Processor and RicksLife joined the live map after 004), (6) five entries, one per solutions
'        row, and no unbound list. (8) added: extract --repo-path on DSP_Processor's repository resolves from the map alone, through a
'        scripted launcher (FR-434). Unarmed: Skipped, as before; the armed Reds are recorded when the live steps run (T041-T045).

' RED:   2026-09-17 (T042) the armed live run after the map was upgraded to schema 3: (6) red - MemOS "behind" where "current" was
'        asserted. Diagnosed: HEAD bf275596 is 2 commits past the recorded cbeb6615, tree clean, and those two commits are MemOS's own
'        map-pin fixpack and its merge - made so MemOS could read a version-3 map at all. The fact was asserting a TRANSIENT; re-pinning
'        it to behind/2 would have gone red again on the next MemOS commit. Reworked (the Architect's direction): (6) now recomputes each
'        entry's inputs from git and asserts the verdict those inputs imply, so it holds at any staleness, and is renamed
'        LiveMapStatusVerdictsAgreeWithGit. MemOS was also re-extracted at the same request (run 13, sha bf275596), which is what P008 of
'        the MemOS fixpack needs. Live coverage on the armed run after the rework, all five entries compared and agreeing: current x2
'        (DSP_Processor, MemOS) and dirty x3 (CodeMem, GameRoom, RicksLife) - CodeMem dirty WITH behindBy 20, which is the one case that
'        exercises the dirty-before-behind precedence. behind, diverged and no_git had no live instance on that run and are covered by the
'        fixture facts, not here; this fact claims only that whatever state the repositories are in, the verdict matches git.
' RED:   2026-09-17 (T042, after the MemOS re-extraction the Architect asked for) (7) red - presented 15545 where active - 277 = 15547.
'        Diagnosed and re-pinned to 279: the fixpack added exactly two members to CodeMemMapFixture.vb, which is LINKED into the
'        integration project as well, so each folds as one twin pair. The arithmetic closes exactly (15,824 - 279 = 15,545); see the
'        comment at the assertion. This figure is expected to move with every MemOS extraction that touches a linked file.
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
    ''' (1) solutions lists five: GameRoom, CodeMem and MemOS with runs 4, 5 and 6 or later, DSP_Processor and RicksLife (005).
    ''' </summary>
    <SkippableFact>
    Public Sub LiveSolutionsListsFive()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "solutions", Args())
        Dim runs As Dictionary(Of String, Long) = New Dictionary(Of String, Long)(StringComparer.Ordinal)
        For Each solution As JsonElement In reply.Root().GetProperty("solutions").EnumerateArray()
            runs(solution.GetProperty("key").GetString()) = solution.GetProperty("latestRun").GetProperty("runId").GetInt64()
        Next
        Assert.True(runs("GameRoom") >= 4, "GameRoom run " & runs("GameRoom"))
        Assert.True(runs("CodeMem") >= 5, "CodeMem run " & runs("CodeMem"))
        Assert.True(runs("MemOS") >= 6, "MemOS run " & runs("MemOS"))
        Assert.True(runs.ContainsKey("DSP_Processor") AndAlso runs.ContainsKey("RicksLife"), "keys: " & String.Join(", ", runs.Keys))
        Assert.Equal(5, runs.Count)
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
    ''' (4) symbol_search by solutionKey GameRoom resolves from the map alone: btnDeal_Click is one row, id 584 (the 004 fact asked by
    ''' projectId 132040 through the registry; 005 FR-405).
    ''' </summary>
    <SkippableFact>
    Public Sub LiveSearchByKeyResolvesFromTheMap()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "symbol_search", Args("solutionKey", "GameRoom", "name", "btnDeal_Click"))
        Assert.Equal("solutionKey", reply.Root().GetProperty("scope").GetProperty("by").GetString())
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
    ''' (6) map_status over the live map: five entries, one per solutions row, no unbound list (SC-304, FR-345; 005 FR-418) - and
    ''' <b>every entry's verdict checked against git read independently</b>, so the fact holds at ANY staleness.
    ''' <para>
    ''' <b>Why it no longer asserts a verdict by name.</b> Until 2026-09-17 this fact asserted MemOS <c>current</c> with
    ''' <c>behindBy</c> 0 - a transient. It went red the moment MemOS took its next commit (T042: HEAD bf275596 two commits past
    ''' the recorded cbeb6615), which is a state the map is ALLOWED to be in, not a defect. Re-pinning the number would have
    ''' bought one day. So the fact now recomputes each entry's inputs from git itself - HEAD, whether the tree is dirty,
    ''' ancestry, and the distance from the recorded commit - and asserts the bridge's verdict is the one those inputs imply
    ''' under the documented precedence (no_git, dirty, diverged, behind, current: MapStatusReader's header).
    ''' </para>
    ''' <para>
    ''' <b>The independent side mirrors the production options deliberately</b>: RepositoryFacts scans with
    ''' <c>ExcludeSubmodules</c>, so the dirty probe passes <c>--ignore-submodules=all</c>. Where the two could disagree for a
    ''' reason that is not a defect, the probe is made to agree on purpose, and said so here.
    ''' </para>
    ''' <para>
    ''' <b>What this proves and what it does not</b> (scope carried into the claim): the recorded commit is taken from the reply,
    ''' so this fact does NOT independently verify what the map holds - (1) and the solutions facts do that. What it does prove
    ''' is that, for the recorded commit, the verdict, HEAD, dirty flag and distance the bridge reports are the ones git gives,
    ''' on every entry, whatever the staleness. A comparison that never ran proves nothing, so the count of entries actually
    ''' compared is asserted non-zero and printed.
    ''' </para>
    ''' </summary>
    <SkippableFact>
    Public Sub LiveMapStatusVerdictsAgreeWithGit()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "map_status", Args())
        Dim root As JsonElement = reply.Root()
        Dim entries As Dictionary(Of String, JsonElement) = New Dictionary(Of String, JsonElement)(StringComparer.Ordinal)
        For Each entry As JsonElement In root.GetProperty("entries").EnumerateArray()
            entries(entry.GetProperty("solutionKey").GetString()) = entry
        Next

        Assert.Equal(5, root.GetProperty("entries").GetArrayLength())
        Assert.Equal(Timed(live, "solutions", Args()).Root().GetProperty("solutions").GetArrayLength(), root.GetProperty("entries").GetArrayLength())
        Assert.False(HasProperty(root, "unbound"))
        For Each key As String In New String() {"GameRoom", "CodeMem", "MemOS"}
            Assert.True(entries.ContainsKey(key), "the live map no longer holds " & key)
        Next

        Dim compared As Integer = 0
        For Each pair As KeyValuePair(Of String, JsonElement) In entries
            Dim entry As JsonElement = pair.Value
            Dim verdict As String = entry.GetProperty("verdict").GetString()

            ' No completed run, or a run that recorded no commit: no_git is the only lawful answer and there is nothing to compare.
            If Not HasProperty(entry, "run") OrElse entry.GetProperty("run").ValueKind = JsonValueKind.Null Then
                Assert.Equal("no_git", verdict)
                Continue For
            End If
            Dim recordedSha As JsonElement = entry.GetProperty("run").GetProperty("commitSha")
            If recordedSha.ValueKind <> JsonValueKind.String Then
                Assert.Equal("no_git", verdict)
                Continue For
            End If

            Dim recorded As String = recordedSha.GetString()
            Dim repoRoot As String = entry.GetProperty("repoRoot").GetString()
            Dim headSha As String = Git(repoRoot, "rev-parse HEAD")
            If headSha Is Nothing Then
                Assert.Equal("no_git", verdict)
                Continue For
            End If

            Dim treeDirty As Boolean = Not String.IsNullOrWhiteSpace(Git(repoRoot, "status --porcelain --ignore-submodules=all"))
            Dim isAncestor As Boolean = GitExitsZero(repoRoot, "merge-base --is-ancestor " & recorded & " HEAD")
            Dim behindBy As Integer = 0
            If isAncestor Then
                Dim counted As String = Git(repoRoot, "rev-list --count " & recorded & "..HEAD")
                If counted IsNot Nothing Then Integer.TryParse(counted, behindBy)
            End If

            Dim expected As String
            If treeDirty Then
                expected = "dirty"
            ElseIf Not isAncestor Then
                expected = "diverged"
            ElseIf behindBy > 0 Then
                expected = "behind"
            Else
                expected = "current"
            End If

            _output.WriteLine(pair.Key & ": bridge " & verdict & " | git HEAD " & headSha.Substring(0, 8) &
                              " dirty=" & treeDirty.ToString() & " ancestor=" & isAncestor.ToString() &
                              " behindBy=" & behindBy.ToString() & " -> expected " & expected &
                              " | " & entry.GetProperty("reason").GetString())

            Assert.Equal(expected, verdict)
            Assert.Equal(headSha, entry.GetProperty("head").GetProperty("sha").GetString())
            Assert.Equal(treeDirty, entry.GetProperty("head").GetProperty("treeDirty").GetBoolean())
            ' diverged claims no count by contract; every other computable verdict states the distance git gives.
            If expected <> "diverged" Then
                Assert.Equal(behindBy, entry.GetProperty("head").GetProperty("behindBy").GetInt32())
            End If
            compared += 1
        Next

        Assert.True(compared >= 1, "no entry was comparable against git, so this fact asserted nothing about any verdict")
        _output.WriteLine("entries compared against git: " & compared.ToString())

        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' Runs git in a repository root and returns its trimmed stdout, or <see langword="Nothing"/> when git cannot be run or
    ''' exits non-zero. The independent side of fact (6); nothing else in this class shells out.
    ''' </summary>
    ''' <param name="repoRoot">The working directory to run in.</param>
    ''' <param name="arguments">The git arguments.</param>
    ''' <returns>Trimmed stdout, or Nothing.</returns>
    Private Shared Function Git(repoRoot As String, arguments As String) As String
        Try
            Dim psi As ProcessStartInfo = New ProcessStartInfo("git", arguments) With {
                .WorkingDirectory = repoRoot, .RedirectStandardOutput = True, .RedirectStandardError = True,
                .UseShellExecute = False, .CreateNoWindow = True}
            Using started As Process = Process.Start(psi)
                Dim output As String = started.StandardOutput.ReadToEnd()
                started.StandardError.ReadToEnd()
                started.WaitForExit()
                If started.ExitCode <> 0 Then Return Nothing
                Return output.Trim()
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>Runs git and reports only whether it exited 0 - for the predicates that answer by exit code.</summary>
    ''' <param name="repoRoot">The working directory to run in.</param>
    ''' <param name="arguments">The git arguments.</param>
    ''' <returns><c>True</c> when git exited 0.</returns>
    Private Shared Function GitExitsZero(repoRoot As String, arguments As String) As Boolean
        Try
            Dim psi As ProcessStartInfo = New ProcessStartInfo("git", arguments) With {
                .WorkingDirectory = repoRoot, .RedirectStandardOutput = True, .RedirectStandardError = True,
                .UseShellExecute = False, .CreateNoWindow = True}
            Using started As Process = Process.Start(psi)
                started.StandardOutput.ReadToEnd()
                started.StandardError.ReadToEnd()
                started.WaitForExit()
                Return started.ExitCode = 0
            End Using
        Catch
            Return False
        End Try
    End Function

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
        ' 279 absorbed, not 277 (re-pinned 2026-09-17, T042): MemOS run 13 added two members to
        ' tests/contract/MemOS.ContractTests/TestSupport/CodeMemMapFixture.vb - Migration2To3Text and Migration2To3Sql - and that file is
        ' <Compile Include=...Link=...> into MemOS.IntegrationTests as well, so each new member is compiled into two projects and folds as
        ' one twin pair: 277 + 2. Checked, not assumed: 15,824 active - 279 = 15,545 presented, the figures the run reported.
        Assert.Equal(active - 279, presented)
        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' (8) extract --repo-path on DSP_Processor's repository resolves to key DSP_Processor from the map alone (005 FR-434; analyze G4): a temp
    ''' configuration with both gates on and a scripted launcher, so nothing real runs; the scripted request's solution path is the map's
    ''' last_seen_path; the live map's hash unchanged.
    ''' </summary>
    <SkippableFact>
    Public Sub ExtractByRepoPathResolvesDspProcessorFromTheMapAlone()
        Dim live As LiveBridge = Arm()
        Dim host As BridgeHost = New BridgeHost(BridgeHost.WriteConfig(live.MapPath, Nothing, True, True))
        host.Launcher.Script(0, "solution=DSP_Processor run_id=0 observed=0", "")
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", "C:\Users\rchau\source\repos\DSP_Processor"))
        Assert.False(reply.IsError, reply.Text)
        Assert.Equal("DSP_Processor", reply.Root().GetProperty("target").GetProperty("resolvedKey").GetString())
        Dim expected As String = MapQueries.ReadSolutions(live.MapPath).Find(Function(row As SolutionRow) row.Key = "DSP_Processor").LastSeenPath
        Assert.Equal(expected, Assert.Single(host.Launcher.Requests).SolutionPath)
        live.AssertUnchanged()
    End Sub

    ''' <summary>
    ''' Arms the facts: the variable set, else Skipped.
    ''' </summary>
    ''' <returns>The host over the live map with the hash taken.</returns>
    Private Shared Function Arm() As LiveBridge
        Dim mapPath As String = Environment.GetEnvironmentVariable("CODEMEM_LIVE_MAP")
        Skip.If(String.IsNullOrEmpty(mapPath), "CODEMEM_LIVE_MAP is not set")
        Return New LiveBridge(mapPath)
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

    Private Shared Function HasProperty(element As JsonElement, name As String) As Boolean
        Dim value As JsonElement
        Return element.TryGetProperty(name, value)
    End Function

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

End Class

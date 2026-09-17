' File: B05_ExtractGateTests.vb
' Project: CodeMem.Tests
' Description: The extract door: the gate matrix by gate name, cardinality before configuration, resolution by directory containment, the key refusals, one extraction at a time, stale, the child's outcomes verbatim, the timeout, the log, one real launch and one gate refusal over stdio (FR-325..FR-334, FR-346, FR-347, FR-351; SC-305, SC-306; INC2, INC3, COR1, TIM1, CON3).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' extract.log lives beside the executable - for the in-process facts that is the test host's AppContext.BaseDirectory, shared by every
' fact and by the spawned executable - so each fact identifies its own lines by the unique path or key it used (LogLines), never by the
' file's length (INC3). B05 and B06 share the ExtractLog collection so xUnit runs them in sequence (analyze pass 2, U2).
' RED:   2026-09-15 (T038) the assembly did not compile: ScriptedLauncher, LaunchRequest, ExtractResult, ExtractRequest, ExtractOrigin not
'        defined, BridgeHost.Launcher / Door / ConfigPath not members, WriteConfig without its path parameter - the battery's named Red with
'        B06 and R01 (f). After T037's skeleton and T041: 17 red on NotImplementedException("US4") from ExtractDoor.Run / RunStale, (15)
'        red on KeyNotFound (extract not registered).
' GREEN: 2026-09-16 (T045) after ExtractGates, HookRequest and the shipped sample config and fragment: B05 (1)-(17), B06 (1)-(8) and
'        B01 (1)-(6) green together - 39 passed / 0 failed in 34 s. One assertion was corrected on the way: (6) had matched the Windows
'        path against the raw JSON text, where backslashes are escaped; it now reads the parsed refusal text.
' FIRE:  2026-09-16 (T046) each fault injected alone, its one fact run, the file restored from a byte copy: (a) ExtractGates let the hook's
'        own gate decide the green-build origin before extract.enabled - (3) red (Launched True, gate passed); (b) ExtractDoor resolved
'        the target before the gates - (5) red (the refusal named the path, not extract.enabled); (d) the semaphore never refused - (10)
'        red ("the second call launched"); (f) a timed-out child reported as exit 0 - (12b) red ("a timeout was not an error on the
'        wire"); (g) TargetResolver used Contains, the file rule, for the directory - (7b) red (the root itself refused as
'        PathNotRegistered); (i) the read transaction held open across the launch - (13) red: the real child exited 1 with "database
'        error: SQLite Error 5: 'database is locked'." and no run row (FR-334, G2). After the last revert: B05 + B06 33 passed / 0 failed
'        in 26 s.
' RED:   2026-09-16 (T055) (18) added after the live step: every call against the sample copied beside the Release executable was
'        refused as Unconfigured - the shipped bridge.config.sample.json carried single backslashes in its paths, not JSON escapes;
'        (18) red on JsonReaderException ("'_' is an invalid escapable character"). GREEN: the sample rewritten with "C:\_DB\..."
'        (18) 1 passed; the live reads then answered (quickstart, Record). Nothing tested the shipped file before this fact.
'
' 2026-09-17 (feature 005, T019): RED - every extract fact red once the store had no path to open (T013: no configuration names one) and,
' with the resolver still registry-bound, (6) and (9) red on their phrases. Amended: the scenario's two registries became two maps
' (Map: Sample; NestedMap: Sample, Inner, Twin - (8) reads NestedHost); (6) expects the not-in-map answer; (9) is the map's one key
' refusal, SolutionKeyUnknown with the add remedy - the unbound / inactive / ghost cases and (17), the empty registry, are cut into
' _Archive/004-store/tests/B05_RetiredFacts.vb; (11)'s registry seed is gone. Green at T021 with the resolver.
' GREEN: 2026-09-17 (T021) 17 of 18 with B10 9 of 9 and B06 14 of 14 once TargetResolver read the map's rows; (18) red on ConfigKeyRetired -
'        the shipped sample still carried storePath (the T022 Red). (18) amended to assert the key absent; the sample rewritten -> green.
' 2026-09-17 (feature 005, T035): the plan named (2) as a Red when warnings=<n> joined the summary line; it was not red - the line (2)
'        asserts is scripted into the launcher and echoed verbatim, never the extractor's. The scripted line now carries warnings=0 so
'        it keeps the current shape; the extractor's own line is asserted by RefusalTests (d) and X02 (1).

Imports System.IO
Imports System.Text.Json
Imports System.Threading
Imports System.Threading.Tasks
Imports CodeMem.Bridging
Imports CodeMem.Extraction
Imports LibGit2Sharp
Imports Xunit

''' <summary>
''' Fast facts through the in-process door with a scripted launcher over one shared extraction (ExtractScenario); the production route is
''' fact (13) through the executable.
''' </summary>
<Collection("ExtractLog")>
Public Class B05_ExtractGateTests
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
    ''' (1) Both gates off: every origin is refused naming extract.enabled, nothing launches, the map is byte-identical, one log line per
    ''' invocation whose gate column is extract.enabled.
    ''' </summary>
    <Fact>
    Public Sub BothGatesOffRefusesNamingEnabled()
        Dim host As BridgeHost = _scenario.Host(False, False)
        Dim probe As String = _scenario.UniquePathUnderRoot()
        Dim before As String = MapSnapshot.FileBytesHash(_scenario.Map.Path)
        Dim tool As BridgeReply = host.Invoke("extract", Args("repoPath", probe))
        Assert.True(tool.IsError, "the tool origin was not refused")
        Assert.Contains("extract.enabled is false", tool.Text)
        For Each origin As ExtractOrigin In New ExtractOrigin() {ExtractOrigin.GreenBuild, ExtractOrigin.Manual}
            Dim result As ExtractResult = host.Door.Run(New ExtractRequest With {.Origin = origin, .RepoPath = probe})
            Assert.False(result.Launched)
            Assert.Equal("extract.enabled", result.Gate)
            Assert.Contains("extract.enabled is false", result.Refusal.Text)
        Next
        Assert.Equal(0, host.Launcher.Requests.Count)
        Assert.Equal(before, MapSnapshot.FileBytesHash(_scenario.Map.Path))
        Dim lines As List(Of String()) = LogLines.Containing(probe)
        Assert.Equal(3, lines.Count)
        For Each line As String() In lines
            Assert.Equal("extract.enabled", line(4))
        Next
    End Sub

    ''' <summary>
    ''' (2) extract.enabled on, extract.onGreenBuild off: the green-build origin is refused naming extract.onGreenBuild with no launch; the
    ''' tool origin runs (scripted exit 0 with the fixture's summary line) and returns the exit code, the line verbatim and the run's ten
    ''' counts equal to the map's row.
    ''' </summary>
    <Fact>
    Public Sub EnabledOnlyRefusesTheHookAndRunsTheTool()
        Dim host As BridgeHost = _scenario.Host(True, False)
        Dim hook As ExtractResult = host.Door.Run(New ExtractRequest With {.Origin = ExtractOrigin.GreenBuild, .RepoPath = _scenario.UniquePathUnderRoot()})
        Assert.False(hook.Launched)
        Assert.Equal("extract.onGreenBuild", hook.Gate)
        Assert.Contains("extract.onGreenBuild is false", hook.Refusal.Text)
        Assert.Equal(0, host.Launcher.Requests.Count)
        Dim row As RunRow = MapQueries.ReadRuns(_scenario.Map.Path).Find(Function(r As RunRow) r.Id = _scenario.FirstRunId)
        Dim summary As String = "solution=Sample run_id=" & row.Id & " observed=" & row.SymbolsObserved & " matched=" & row.SymbolsMatched & " new=" & row.SymbolsNew & " sha=null warnings=0"
        host.Launcher.Script(0, summary & vbLf, "")
        Dim tool As BridgeReply = host.Invoke("extract", Args("solutionKey", "Sample"))
        Assert.False(tool.IsError, tool.Text)
        Dim root As JsonElement = tool.Root()
        Assert.True(root.GetProperty("launched").GetBoolean())
        Assert.Equal(0, root.GetProperty("exitCode").GetInt32())
        Assert.Equal(summary, root.GetProperty("line").GetString())
        Dim run As JsonElement = root.GetProperty("run")
        Assert.Equal(row.Id, run.GetProperty("runId").GetInt64())
        Assert.Equal(row.SymbolsObserved, run.GetProperty("symbolsObserved").GetInt32())
        Assert.Equal(row.SymbolsMatched, run.GetProperty("symbolsMatched").GetInt32())
        Assert.Equal(row.SymbolsReactivated, run.GetProperty("symbolsReactivated").GetInt32())
        Assert.Equal(row.SymbolsNew, run.GetProperty("symbolsNew").GetInt32())
        Assert.Equal(row.SymbolsRetired, run.GetProperty("symbolsRetired").GetInt32())
        Assert.Equal(row.RegistryActiveBefore, run.GetProperty("registryActiveBefore").GetInt32())
        Assert.Equal(row.NotesOrphaned, run.GetProperty("notesOrphaned").GetInt32())
        Assert.Equal(row.RenameCandidates, run.GetProperty("renameCandidates").GetInt32())
        Assert.Equal(row.UnaccountedObserved, run.GetProperty("unaccountedObserved").GetInt32())
        Assert.Equal(row.UnaccountedRegistry, run.GetProperty("unaccountedRegistry").GetInt32())
        Assert.Equal(1, host.Launcher.Requests.Count)
    End Sub

    ''' <summary>
    ''' (3) extract.onGreenBuild on but extract.enabled off: the second gate is inert; the refusal names extract.enabled and nothing launches.
    ''' </summary>
    <Fact>
    Public Sub OnGreenBuildOnlyRefusesNamingEnabled()
        Dim host As BridgeHost = _scenario.Host(False, True)
        Dim result As ExtractResult = host.Door.Run(New ExtractRequest With {.Origin = ExtractOrigin.GreenBuild, .RepoPath = _scenario.UniquePathUnderRoot()})
        Assert.False(result.Launched)
        Assert.Equal("extract.enabled", result.Gate)
        Assert.Contains("extract.enabled", result.Refusal.Text)
        Assert.Equal(0, host.Launcher.Requests.Count)
    End Sub

    ''' <summary>
    ''' (4) Both gates on: every origin launches, three launches and three log lines.
    ''' </summary>
    <Fact>
    Public Sub BothOnRunsEveryOrigin()
        Dim host As BridgeHost = _scenario.Host(True, True)
        Dim probe As String = _scenario.UniquePathUnderRoot()
        Assert.False(host.Invoke("extract", Args("repoPath", probe)).IsError)
        For Each origin As ExtractOrigin In New ExtractOrigin() {ExtractOrigin.GreenBuild, ExtractOrigin.Manual}
            Dim result As ExtractResult = host.Door.Run(New ExtractRequest With {.Origin = origin, .RepoPath = probe})
            Assert.True(result.Launched, origin.ToString() & ": " & If(result.Refusal Is Nothing, "", result.Refusal.Text))
            Assert.Equal("passed", result.Gate)
        Next
        Assert.Equal(3, host.Launcher.Requests.Count)
        Assert.Equal(3, LogLines.Containing(probe).Count)
    End Sub

    ''' <summary>
    ''' (5) The gate is evaluated before target resolution: both off and a path under no root names extract.enabled, not the path.
    ''' </summary>
    <Fact>
    Public Sub GateBeforeResolution()
        Dim host As BridgeHost = _scenario.Host(False, False)
        Dim nowhere As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "nowhere-" & Guid.NewGuid().ToString("N"))
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", nowhere))
        Assert.True(reply.IsError)
        Assert.Contains("extract.enabled", reply.Text)
        Assert.DoesNotContain("No registered solution", reply.Text)
    End Sub

    ''' <summary>
    ''' (5b) Cardinality is validated before the configuration is read (INC2): none of the three targets and an absent config file refuse
    ''' naming the three, not the configuration.
    ''' </summary>
    <Fact>
    Public Sub CardinalityBeforeConfiguration()
        Dim absentConfig As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "absent-config-" & Guid.NewGuid().ToString("N") & ".json")
        Dim reply As BridgeReply = New BridgeHost(absentConfig).Invoke("extract", Args())
        Assert.True(reply.IsError)
        Assert.Contains("exactly one of solutionKey, repoPath or stale", reply.Text)
        Assert.DoesNotContain("not configured", reply.Text)
    End Sub

    ''' <summary>
    ''' (6) A repoPath under no mapped root is answered not in the map naming the path (FR-347; 005 FR-411): no launch, the map byte-identical,
    ''' one log line resolved as PathNotInMap.
    ''' </summary>
    <Fact>
    Public Sub PathUnderNoRootRefusesNamingThePath()
        Dim host As BridgeHost = _scenario.Host(True, True)
        Dim nowhere As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "nowhere-" & Guid.NewGuid().ToString("N"))
        Dim before As String = MapSnapshot.FileBytesHash(_scenario.Map.Path)
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", nowhere))
        Assert.True(reply.IsError)
        Assert.Contains("not in the map", reply.Text)
        Assert.Contains(nowhere, reply.Root().GetProperty("refusal").GetProperty("text").GetString())
        Assert.Equal(0, host.Launcher.Requests.Count)
        Assert.Equal(before, MapSnapshot.FileBytesHash(_scenario.Map.Path))
        Assert.Equal(1, LogLines.Containing(nowhere).Count)
    End Sub

    ''' <summary>
    ''' (7) A repoPath under the registered root resolves to Sample; the launch request carries the key, the map's last-seen path, the
    ''' configured map and the 540 s budget.
    ''' </summary>
    <Fact>
    Public Sub PathUnderTheRootResolvesToTheKey()
        Dim host As BridgeHost = _scenario.Host(True, True)
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", Path.Combine(_scenario.Copy.Directory, "Sample.Lib")))
        Assert.False(reply.IsError, reply.Text)
        Assert.Equal("Sample", reply.Root().GetProperty("target").GetProperty("resolvedKey").GetString())
        Dim request As LaunchRequest = Assert.Single(host.Launcher.Requests)
        Assert.Equal("Sample", request.SolutionKey)
        Assert.Equal(MapQueries.ReadSolutions(_scenario.Map.Path).Find(Function(s As SolutionRow) s.Key = "Sample").LastSeenPath, request.SolutionPath)
        Assert.Equal(_scenario.Map.Path, request.MapPath)
        Assert.Equal(TimeSpan.FromSeconds(540), request.Budget)
    End Sub

    ''' <summary>
    ''' (7b) The root itself resolves (COR1): with and without its trailing separator.
    ''' </summary>
    <Fact>
    Public Sub TheRootItselfResolves()
        Dim host As BridgeHost = _scenario.Host(True, True)
        For Each root As String In New String() {_scenario.Root, Path.TrimEndingDirectorySeparator(_scenario.Root)}
            Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", root))
            Assert.False(reply.IsError, reply.Text)
            Assert.Equal("Sample", reply.Root().GetProperty("target").GetProperty("resolvedKey").GetString())
        Next
    End Sub

    ''' <summary>
    ''' (8) Nested roots pick the longest; the same root bound twice is ambiguous, naming both keys.
    ''' </summary>
    <Fact>
    Public Sub NestedRootsPickTheLongestAndSameRootTwiceIsAmbiguous()
        Dim host As BridgeHost = _scenario.NestedHost(True, True)
        Dim nested As BridgeReply = host.Invoke("extract", Args("repoPath", Path.Combine(_scenario.Copy.Directory, "Sample.Lib")))
        Assert.False(nested.IsError, nested.Text)
        Assert.Equal("Inner", nested.Root().GetProperty("target").GetProperty("resolvedKey").GetString())
        Dim tie As BridgeReply = host.Invoke("extract", Args("repoPath", Path.Combine(_scenario.Root, "Elsewhere")))
        Assert.True(tie.IsError, "the tie was not refused")
        Assert.Contains("more than one", tie.Text)
        Assert.Contains("Sample", tie.Text)
        Assert.Contains("Twin", tie.Text)
        Assert.Equal(1, host.Launcher.Requests.Count)
    End Sub

    ''' <summary>
    ''' (9) A key the map lacks is refused naming it, with the add remedy (005 FR-415); no launch, the map byte-identical (SC-306).
    ''' </summary>
    <Fact>
    Public Sub KeyRefusals()
        Dim host As BridgeHost = _scenario.Host(True, True)
        Dim before As String = MapSnapshot.FileBytesHash(_scenario.Map.Path)
        Dim reply As BridgeReply = host.Invoke("extract", Args("solutionKey", "Nope"))
        Expect("unknown key", reply, "holds no solution with key")
        Assert.Contains("extract --solution-key Nope --solution", reply.Text, StringComparison.Ordinal)
        Assert.Equal(0, host.Launcher.Requests.Count)
        Assert.Equal(before, MapSnapshot.FileBytesHash(_scenario.Map.Path))
    End Sub

    ''' <summary>
    ''' (10) A second call while one runs is refused by name: two calls on two threads with a 2 s launcher delay, one launched, one still running.
    ''' </summary>
    <Fact>
    Public Sub SecondCallWhileRunningIsRefused()
        Dim host As BridgeHost = _scenario.Host(True, True)
        host.Launcher.Delay = TimeSpan.FromSeconds(2)
        Dim first As Task(Of ExtractResult) = Task.Run(Function() host.Door.Run(New ExtractRequest With {.Origin = ExtractOrigin.Tool, .SolutionKey = "Sample"}))
        Thread.Sleep(400)
        Dim second As ExtractResult = host.Door.Run(New ExtractRequest With {.Origin = ExtractOrigin.Manual, .SolutionKey = "Sample"})
        Dim firstResult As ExtractResult = first.Result
        Assert.True(firstResult.Launched, "the first call did not launch")
        Assert.False(second.Launched, "the second call launched")
        Assert.Contains("still running", second.Refusal.Text)
        Assert.Equal(1, host.Launcher.Requests.Count)
    End Sub

    ''' <summary>
    ''' (11) stale on three bound solutions - current, behind, dirty - considers three, extracts two in sequence, lists the current one with
    ''' its verdict, and writes exactly one log line per solution launched and no header (INC3).
    ''' </summary>
    <Fact>
    Public Sub StaleExtractsTheBehindAndDirtyOnes()
        Dim tag As String = Guid.NewGuid().ToString("N").Substring(0, 8)
        Dim copies As List(Of FixtureCopy) = New List(Of FixtureCopy)()
        Using map As TempMap = New TempMap()
            Try
                For Each state As String In New String() {"current", "behind", "dirty"}
                    Dim copy As FixtureCopy = New FixtureCopy()
                    copies.Add(copy)
                    Dim key As String = "Stale" & tag & state
                    Using repo As Repository = GitFixture.Init(copy.ParentDirectory)
                        GitFixture.CommitAll(repo, "c1")
                        Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path, .SolutionKey = key}, Nothing))
                        If state = "behind" Then
                            GitFixture.Touch(copy.ParentDirectory, "Sample/Sample.Lib/Extra.vb", "Public Class Extra" & vbLf & "End Class" & vbLf)
                            GitFixture.CommitAll(repo, "c2")
                        ElseIf state = "dirty" Then
                            File.AppendAllText(Path.Combine(copy.Directory, "Sample.Lib", "Consumer.vb"), "' dirty" & vbLf)
                        End If
                    End Using
                Next
                Dim host As BridgeHost = New BridgeHost(BridgeHost.WriteConfig(map.Path, Nothing, True, True))
                Dim reply As BridgeReply = host.Invoke("extract", Args("stale", True))
                Assert.False(reply.IsError, reply.Text)
                Dim root As JsonElement = reply.Root()
                Assert.Equal(3, root.GetProperty("considered").GetArrayLength())
                Assert.Equal(2, root.GetProperty("extractedCount").GetInt32())
                For Each entry As JsonElement In root.GetProperty("considered").EnumerateArray()
                    Dim key As String = entry.GetProperty("solutionKey").GetString()
                    If key.EndsWith("current", StringComparison.Ordinal) Then
                        Assert.False(entry.GetProperty("extracted").GetBoolean())
                        Assert.Equal("current", entry.GetProperty("verdict").GetString())
                        Assert.Equal(JsonValueKind.Null, entry.GetProperty("result").ValueKind)
                    Else
                        Assert.True(entry.GetProperty("extracted").GetBoolean(), key)
                        Assert.True(entry.GetProperty("result").GetProperty("launched").GetBoolean(), key)
                    End If
                Next
                Assert.Equal(2, host.Launcher.Requests.Count)
                Dim lines As List(Of String()) = LogLines.Containing("Stale" & tag)
                Assert.Equal(2, lines.Count)
                For Each line As String() In lines
                    Assert.Equal("stale", line(2))
                    Assert.Equal("passed", line(4))
                Next
            Finally
                For Each copy As FixtureCopy In copies
                    copy.Dispose()
                Next
            End Try
        End Using
    End Sub

    ''' <summary>
    ''' (12) The child's exit codes and lines are reported verbatim: exit 2 with a stderr line; exit 4 with the prefixed summary; run null;
    ''' IsError on the wire.
    ''' </summary>
    <Fact>
    Public Sub ChildExitCodesAreReportedVerbatim()
        Dim host As BridgeHost = _scenario.Host(True, True)
        host.Launcher.Script(2, "", "errors=2" & vbLf)
        Dim compileErrors As BridgeReply = host.Invoke("extract", Args("solutionKey", "Sample"))
        Assert.True(compileErrors.IsError, "exit 2 was not an error on the wire")
        Assert.Equal(2, compileErrors.Root().GetProperty("exitCode").GetInt32())
        Assert.Equal("errors=2", compileErrors.Root().GetProperty("line").GetString())
        Assert.Equal(JsonValueKind.Null, compileErrors.Root().GetProperty("run").ValueKind)
        host.Launcher.Script(4, "", "outcome=failed solution=Sample run_id=999 observed=1 unaccounted_observed=-1" & vbLf)
        Dim residual As BridgeReply = host.Invoke("extract", Args("solutionKey", "Sample"))
        Assert.True(residual.IsError)
        Assert.Equal(4, residual.Root().GetProperty("exitCode").GetInt32())
        Assert.Equal("outcome=failed solution=Sample run_id=999 observed=1 unaccounted_observed=-1", residual.Root().GetProperty("line").GetString())
        Assert.Equal(JsonValueKind.Null, residual.Root().GetProperty("run").ValueKind)
    End Sub

    ''' <summary>
    ''' (12b) A child that exceeds the budget is reported as timed out, never as a success (TIM1): timedOut true, exitCode null, the line names
    ''' 540 s, run null, IsError, the log's exit column timeout.
    ''' </summary>
    <Fact>
    Public Sub ATimedOutChildIsReportedNotHidden()
        Dim host As BridgeHost = _scenario.Host(True, True)
        Dim probe As String = _scenario.UniquePathUnderRoot()
        host.Launcher.ScriptTimeout()
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", probe))
        Assert.True(reply.IsError, "a timeout was not an error on the wire")
        Dim root As JsonElement = reply.Root()
        Assert.True(root.GetProperty("timedOut").GetBoolean())
        Assert.Equal(JsonValueKind.Null, root.GetProperty("exitCode").ValueKind)
        Assert.Contains("540", root.GetProperty("line").GetString())
        Assert.Equal(JsonValueKind.Null, root.GetProperty("run").ValueKind)
        Dim line As String() = Assert.Single(LogLines.Containing(probe))
        Assert.Equal("timeout", line(5))
    End Sub

    ''' <summary>
    ''' (13) The production route (CON3): through the spawned executable with the real launcher, extract(solutionKey Sample) runs the
    ''' extractor beside the tests - exit 0, the summary line, the run equal to the map's newest row, elapsedMs above 0; and with both
    ''' gates off the same route refuses naming extract.enabled.
    ''' </summary>
    <Fact>
    Public Sub OneRealLaunchOnTheFixtureOverStdioAndAGateRefusalOverStdio()
        Using server As BridgeProcess = BridgeProcess.Serve(_scenario.Config(True, True))
            server.Initialize()
            Dim reply As ToolReply = server.CallTool("extract", "{""solutionKey"":""Sample""}")
            Assert.False(reply.IsError, reply.Text)
            Dim root As JsonElement = reply.Root()
            Assert.True(root.GetProperty("launched").GetBoolean())
            Assert.Equal(0, root.GetProperty("exitCode").GetInt32())
            Assert.StartsWith("solution=Sample run_id=", root.GetProperty("line").GetString())
            Dim runs As List(Of RunRow) = MapQueries.ReadRuns(_scenario.Map.Path)
            Dim newest As RunRow = runs(runs.Count - 1)
            Assert.Equal(newest.Id, root.GetProperty("run").GetProperty("runId").GetInt64())
            Assert.Equal(newest.SymbolsObserved, root.GetProperty("run").GetProperty("symbolsObserved").GetInt32())
            Assert.True(root.GetProperty("elapsedMs").GetInt64() > 0)
            Assert.Equal(0, server.Close())
        End Using
        Using server As BridgeProcess = BridgeProcess.Serve(_scenario.Config(False, False))
            server.Initialize()
            Dim refused As ToolReply = server.CallTool("extract", "{""solutionKey"":""Sample""}")
            Assert.True(refused.IsError, "the gate did not refuse over stdio")
            Assert.Contains("extract.enabled", refused.Text)
            Assert.Equal(0, server.Close())
        End Using
    End Sub

    ''' <summary>
    ''' (14) A log line that cannot be written never stops the run: with extract.log held open exclusively, logged false, logError named,
    ''' launched true.
    ''' </summary>
    <Fact>
    Public Sub ALogLineThatCannotBeWrittenDoesNotStopTheRun()
        Dim host As BridgeHost = _scenario.Host(True, True)
        Using locked As FileStream = New FileStream(LogLines.LogPath(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
            Dim result As ExtractResult = host.Door.Run(New ExtractRequest With {.Origin = ExtractOrigin.Tool, .SolutionKey = "Sample"})
            Assert.True(result.Launched)
            Assert.False(result.Logged)
            Assert.False(String.IsNullOrEmpty(result.LogError), "no log error named")
        End Using
    End Sub

    ''' <summary>
    ''' (15) The registered description names the two gates, says Gated and that the bridge never writes the map.
    ''' </summary>
    <Fact>
    Public Sub TheExtractDescriptionNamesTheGates()
        Dim description As String = BridgeTools.RegisteredToolDescriptions("extract")
        For Each phrase As String In New String() {"extract.enabled", "extract.onGreenBuild", "Gated", "never writes the map"}
            Assert.Contains(phrase, description)
        Next
    End Sub

    ''' <summary>
    ''' (16) The gate is read per call: one host, both on, one launch; the config file rewritten with extract.enabled false; the next call
    ''' refused naming extract.enabled with no further launch.
    ''' </summary>
    <Fact>
    Public Sub TheGateIsReadPerCall()
        Dim host As BridgeHost = _scenario.Host(True, True)
        Assert.False(host.Invoke("extract", Args("solutionKey", "Sample")).IsError)
        Assert.Equal(1, host.Launcher.Requests.Count)
        BridgeHost.WriteConfig(_scenario.Map.Path, Nothing, False, True, host.ConfigPath)
        Dim refused As BridgeReply = host.Invoke("extract", Args("solutionKey", "Sample"))
        Assert.True(refused.IsError)
        Assert.Contains("extract.enabled", refused.Text)
        Assert.Equal(1, host.Launcher.Requests.Count)
    End Sub

    Private Shared Sub Expect(caseName As String, reply As BridgeReply, phrase As String)
        Assert.True(reply.IsError, caseName & ": not refused; got " & reply.Text)
        Assert.True(reply.Text.Contains(phrase, StringComparison.Ordinal), caseName & ": expected '" & phrase & "' in: " & reply.Text)
    End Sub

    ''' <summary>
    ''' (18) The shipped sample configuration and settings fragment parse as JSON and say what the contract says (cli-config-hook.md §2,
    ''' §4): the two live paths, extractorPath null, both gates false; the PostToolUse command hook on Bash with the bridge's hook entry
    ''' and timeout 600; the bridge's own reader loads the sample with both gates off. Added at T055: the live step found the sample
    ''' unparseable (single backslashes in its paths) and every call against the config beside the Release executable refused as
    ''' Unconfigured.
    ''' </summary>
    <Fact>
    Public Sub TheShippedSampleAndFragmentParseAndMatchTheContract()
        Dim samplePath As String = Path.Combine(RepoPaths.RepositoryRoot(), "src", "CodeMem.Bridge", "bridge.config.sample.json")
        Dim fragmentPath As String = Path.Combine(RepoPaths.RepositoryRoot(), "src", "CodeMem.Bridge", "hooks", "settings.fragment.json")
        Using sample As JsonDocument = JsonDocument.Parse(File.ReadAllText(samplePath))
            Dim root As JsonElement = sample.RootElement
            Assert.Equal("C:\_DB\codemem.sqlite", root.GetProperty("mapPath").GetString())
            Dim retired As JsonElement
            Assert.False(root.TryGetProperty("storePath", retired), "the shipped sample still carries storePath (005 FR-403)")
            Assert.Equal(JsonValueKind.Null, root.GetProperty("extractorPath").ValueKind)
            Assert.False(root.GetProperty("extract").GetProperty("enabled").GetBoolean())
            Assert.False(root.GetProperty("extract").GetProperty("onGreenBuild").GetBoolean())
        End Using
        Dim loaded As BridgeConfig = BridgeConfigFile.Load(samplePath)
        Assert.False(loaded.ExtractEnabled)
        Assert.False(loaded.ExtractOnGreenBuild)
        Assert.Equal("C:\_DB\codemem.sqlite", loaded.MapPath)
        Using fragment As JsonDocument = JsonDocument.Parse(File.ReadAllText(fragmentPath))
            Dim postToolUse As JsonElement = fragment.RootElement.GetProperty("hooks").GetProperty("PostToolUse")(0)
            Assert.Equal("Bash", postToolUse.GetProperty("matcher").GetString())
            Dim hookEntry As JsonElement = postToolUse.GetProperty("hooks")(0)
            Assert.Equal("command", hookEntry.GetProperty("type").GetString())
            Assert.EndsWith("CodeMem.Bridge.exe"" hook", hookEntry.GetProperty("command").GetString())
            Assert.Equal(600, hookEntry.GetProperty("timeout").GetInt32())
        End Using
    End Sub

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

End Class

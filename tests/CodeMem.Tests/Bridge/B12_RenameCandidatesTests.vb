' File: B12_RenameCandidatesTests.vb
' Project: CodeMem.Tests
' Description: rename_candidates (feature 006, US1; FR-501-FR-509, FR-515; data-model §2-§4): runs newest first with the count check, bare ids per twin with each side's project, the retired-symbol run list, the three refusals and the reused symbol ones, read-only, SymbolRetired handing its id over.
' Author: RCH Automation LLC
' Created: 2026-09-29
'
' Every id comes from RenameScenario, never a literal. (3), (5) and (6)'s annotation go through the executable (CON3); the rest through
' the in-process host.
' RED:   2026-09-29 (T007) with the product at 637e96d (the US1 slices stashed): the assembly does not compile - BridgeHost.vb BC30456
'        RenameCandidates is not a member of BridgeTools - recorded as the Red now that the nine facts are written (CON2).
' GREEN: 2026-09-29 (T014) 9 of 9 on the first build with T008-T013 (Core reads, RequireInScope, three kinds, envelopes, reader, registration).
' FIRE:  2026-09-29 (T014) each injected alone, its one fact run, the file restored from a byte copy:
'        (1) fold candidates sharing name, path and line -> red: the fold broke the count, CandidateCountMismatch recorded 3, holds 2;
'        (2) list every run under retiredSymbolId -> red: the run list differs from [run 2];
'        (4) skip the count check -> red: the disagreeing run was answered;
'        (5) drop the solution comparison -> red: Other's run answered under Sample; answer an unknown runId empty -> red: not refused;
'        (6) register ReadOnly = False -> red: readOnlyHint false;
'        FR-509 - Dim probe As SqliteConnection = New SqliteConnection(...) in RenameCandidatesReader.vb (source scan) -> the standalone
'        gate (1) red naming RenameCandidatesReader.vb:39 and OnlyMapDatabaseOpensAConnection red. A fully qualified
'        New Microsoft.Data.Sqlite.SqliteConnection(...) turned only the standalone gate red: the connection-site gate matches the text
'        New SqliteConnection - a finding raised with the Architect, not changed here.

Imports System.Text.Json
Imports CodeMem.Bridging
Imports Xunit

''' <summary>
''' Nine facts on RenameScenario: Sample runs 1-3 (run 2 with three candidates, run 3 failed with one recorded) and Other run 1.
''' </summary>
<Collection("Fixture")>
Public Class B12_RenameCandidatesTests
    Implements IClassFixture(Of RenameScenario)

    Private Const DescribeId As String = "M:Sample.Widgets.BaseWidget.Describe"
    Private Const LabelInLib As String = "M:Sample.Lib.Shared.Twin.Label"
    Private Const RetiredSentence As String = "No rename candidate names this symbol in the runs examined."
    Private Const UnfilteredSentence As String = "No rename candidate was recorded in the runs examined."

    Private ReadOnly _scenario As RenameScenario

    ''' <summary>
    ''' Receives the shared scenario.
    ''' </summary>
    ''' <param name="scenario">The class fixture.</param>
    Public Sub New(scenario As RenameScenario)
        _scenario = scenario
    End Sub

    ''' <summary>
    ''' (1) Unfiltered: every Sample run newest first, run 2 checked 3 = 3, run 1 checked 0 = 0, run 3 failed and not checked; three
    ''' candidates, the two Label ones bare - distinct ids, projects and containers, one name, path and line (Q1).
    ''' </summary>
    <Fact>
    Public Sub UnfilteredListsEveryRunAndTheTwinsAsSeparateCandidates()
        Dim root As JsonElement = Answer(Args("solutionKey", "Sample"))
        Assert.Equal(New Long() {_scenario.SampleRun3Failed, _scenario.SampleRun2, _scenario.SampleRun1}, RunIds(root))
        Dim run2 As JsonElement = RunOf(root, _scenario.SampleRun2)
        Assert.True(run2.GetProperty("countChecked").GetBoolean())
        Assert.Equal(3, run2.GetProperty("renameCandidatesRecorded").GetInt32())
        Assert.Equal(3, run2.GetProperty("candidatesReturned").GetInt32())
        Dim run1 As JsonElement = RunOf(root, _scenario.SampleRun1)
        Assert.True(run1.GetProperty("countChecked").GetBoolean())
        Assert.Equal(0, run1.GetProperty("renameCandidatesRecorded").GetInt32())
        Dim run3 As JsonElement = RunOf(root, _scenario.SampleRun3Failed)
        Assert.Equal("failed", run3.GetProperty("outcome").GetString())
        Assert.Equal(1, run3.GetProperty("renameCandidatesRecorded").GetInt32())
        Assert.Equal(0, run3.GetProperty("candidatesReturned").GetInt32())
        Assert.False(run3.GetProperty("countChecked").GetBoolean())
        Assert.Equal("failed run", run3.GetProperty("countNotCheckedReason").GetString())
        Assert.Equal(3, root.GetProperty("total").GetInt32())
        Assert.Equal(JsonValueKind.Null, root.GetProperty("note").ValueKind)

        Dim labels As List(Of JsonElement) = New List(Of JsonElement)()
        For Each candidate As JsonElement In root.GetProperty("candidates").EnumerateArray()
            Assert.Equal(_scenario.SampleRun2, candidate.GetProperty("runId").GetInt64())
            Assert.True(candidate.GetProperty("samePath").GetBoolean())
            Assert.Equal(JsonValueKind.Number, candidate.GetProperty("offsetDistance").ValueKind)
            If candidate.GetProperty("new").GetProperty("name").GetString() = "Label" Then labels.Add(candidate)
        Next
        Assert.Equal(2, labels.Count)
        Dim a As JsonElement = labels(0).GetProperty("retired")
        Dim b As JsonElement = labels(1).GetProperty("retired")
        Assert.NotEqual(a.GetProperty("id").GetInt64(), b.GetProperty("id").GetInt64())
        Assert.NotEqual(labels(0).GetProperty("new").GetProperty("id").GetInt64(), labels(1).GetProperty("new").GetProperty("id").GetInt64())
        Assert.NotEqual(a.GetProperty("container").GetProperty("id").GetInt64(), b.GetProperty("container").GetProperty("id").GetInt64())
        Dim projects As List(Of String) = New List(Of String) From {a.GetProperty("project").GetProperty("name").GetString(), b.GetProperty("project").GetProperty("name").GetString()}
        projects.Sort(StringComparer.Ordinal)
        Assert.Equal(New String() {"Sample.App", "Sample.Lib"}, projects.ToArray())
        Assert.Equal(a.GetProperty("name").GetString(), b.GetProperty("name").GetString())
        Assert.Equal(a.GetProperty("path").GetString(), b.GetProperty("path").GetString())
        Assert.Equal(a.GetProperty("line").GetInt32(), b.GetProperty("line").GetInt32())
    End Sub

    ''' <summary>
    ''' (2) retiredSymbolId = the retired Describe: one candidate (to Explain), runs exactly [run 2], the symbol retired, last seen in run 1
    ''' and retired in run 2 - the derivation of Q2 - and no run checked.
    ''' </summary>
    <Fact>
    Public Sub RetiredSymbolIdListsOnlyTheRunsThatMatter()
        Dim describe As Long = _scenario.SymbolIdByDocId(_scenario.SampleSolutionId, DescribeId, False)
        Dim root As JsonElement = Answer(Args("solutionKey", "Sample", "retiredSymbolId", describe))
        Assert.Equal(1, root.GetProperty("total").GetInt32())
        Assert.Equal("Explain", root.GetProperty("candidates")(0).GetProperty("new").GetProperty("name").GetString())
        Assert.Equal(New Long() {_scenario.SampleRun2}, RunIds(root))
        Dim symbol As JsonElement = root.GetProperty("symbol")
        Assert.False(symbol.GetProperty("isActive").GetBoolean())
        Assert.Equal(_scenario.SampleRun1, symbol.GetProperty("lastSeenRunId").GetInt64())
        Assert.Equal(_scenario.SampleRun2, symbol.GetProperty("retiredInRunId").GetInt64())
        Dim run As JsonElement = root.GetProperty("runs")(0)
        Assert.False(run.GetProperty("countChecked").GetBoolean())
        Assert.Equal("filtered by retiredSymbolId", run.GetProperty("countNotCheckedReason").GetString())
    End Sub

    ''' <summary>
    ''' (3) Through the executable: symbol_detail on the retired Describe is refused with a text that names the tool and the id, and
    ''' rename_candidates answers that same id (FR-515; the production route of the hand-over).
    ''' </summary>
    <Fact>
    Public Sub TheRetiredRefusalNamesTheToolAndItAnswers()
        Dim describe As Long = _scenario.SymbolIdByDocId(_scenario.SampleSolutionId, DescribeId, False)
        Dim id As String = describe.ToString(Globalization.CultureInfo.InvariantCulture)
        Using server As BridgeProcess = BridgeProcess.Serve(_scenario.ConfigPath)
            server.Initialize()
            Dim refused As ToolReply = server.CallTool("symbol_detail", "{""symbolId"":" & id & ",""solutionKey"":""Sample""}")
            Assert.True(refused.IsError, "a retired symbol was not refused: " & refused.Text)
            Assert.Contains("call rename_candidates with retiredSymbolId " & id, refused.Text, StringComparison.Ordinal)
            Dim answered As ToolReply = server.CallTool("rename_candidates", "{""solutionKey"":""Sample"",""retiredSymbolId"":" & id & "}")
            Assert.False(answered.IsError, answered.Text)
            Assert.Equal(1, answered.Root().GetProperty("total").GetInt32())
            Assert.Equal(0, server.Close())
        End Using
    End Sub

    ''' <summary>
    ''' (4) On a copy with one of run 2's rows removed, asking for run 2 whole is refused by name with both counts (FR-505).
    ''' </summary>
    <Fact>
    Public Sub ACompletedRunThatDisagreesWithItsCountIsRefusedByName()
        Using copy As TempMap = _scenario.CopyMap()
            MapQueries.DeleteCandidate(copy.Path, MapQueries.ReadCandidates(copy.Path, _scenario.SampleRun2)(0).Id)
            Dim host As BridgeHost = New BridgeHost(BridgeHost.WriteConfig(copy.Path, Nothing, False, False))
            Dim reply As BridgeReply = host.Invoke("rename_candidates", Args("solutionKey", "Sample", "runId", _scenario.SampleRun2))
            Assert.True(reply.IsError, "a run whose rows disagree with its count was answered: " & reply.Text)
            Assert.Contains("recorded 3 rename candidates but the map holds 2", reply.Text, StringComparison.Ordinal)
            Assert.Contains("Extract run " & _scenario.SampleRun2.ToString(Globalization.CultureInfo.InvariantCulture), reply.Text, StringComparison.Ordinal)
        End Using
    End Sub

    ''' <summary>
    ''' (5) Through the executable, each refused by name: an unknown run, a run of Other asked under Sample (both keys named), an unknown
    ''' symbol, a symbol of Other asked under Sample.
    ''' </summary>
    <Fact>
    Public Sub RunAndSymbolRefusalsAreByName()
        Dim runs As List(Of RunRow) = MapQueries.ReadRuns(_scenario.Map.Path)
        Dim unknownRun As Long = runs(runs.Count - 1).Id + 1000
        Dim otherSymbol As Long = _scenario.SymbolIdByDocId(_scenario.OtherSolutionId, LabelInLib.Replace(".Label", ".Name"), True)
        Dim unknownSymbol As Long = otherSymbol + 1000000
        Using server As BridgeProcess = BridgeProcess.Serve(_scenario.ConfigPath)
            server.Initialize()
            Refused(server, "{""solutionKey"":""Sample"",""runId"":" & unknownRun & "}", "holds no extract run with id " & unknownRun)
            Dim foreign As ToolReply = Refused(server, "{""solutionKey"":""Sample"",""runId"":" & _scenario.OtherRun1 & "}", "belongs to solution 'Other'")
            Assert.Contains("not to 'Sample'", foreign.Text, StringComparison.Ordinal)
            Refused(server, "{""solutionKey"":""Sample"",""retiredSymbolId"":" & unknownSymbol & "}", "holds no symbol with id")
            Refused(server, "{""solutionKey"":""Sample"",""retiredSymbolId"":" & otherSymbol & "}", "which is not in the requested scope")
            Assert.Equal(0, server.Close())
        End Using
    End Sub

    ''' <summary>
    ''' (6) The map's bytes are unchanged across the tool's calls, and tools/list annotates rename_candidates read-only (FR-507).
    ''' </summary>
    <Fact>
    Public Sub TheToolIsReadOnly()
        Dim describe As Long = _scenario.SymbolIdByDocId(_scenario.SampleSolutionId, DescribeId, False)
        Dim before As String = MapSnapshot.FileBytesHash(_scenario.Map.Path)
        Answer(Args("solutionKey", "Sample"))
        Answer(Args("solutionKey", "Sample", "runId", _scenario.SampleRun2))
        Answer(Args("solutionKey", "Sample", "retiredSymbolId", describe))
        Using server As BridgeProcess = BridgeProcess.Serve(_scenario.ConfigPath)
            server.Initialize()
            Dim annotations As Dictionary(Of String, JsonElement) = server.ListToolAnnotations()
            Assert.True(annotations.ContainsKey("rename_candidates"), "rename_candidates carries no annotations")
            Assert.True(annotations("rename_candidates").GetProperty("readOnlyHint").GetBoolean())
            Assert.Equal(0, server.Close())
        End Using
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools()
        Assert.Equal(before, MapSnapshot.FileBytesHash(_scenario.Map.Path))
    End Sub

    ''' <summary>
    ''' (7) An active symbol's id is answered, not refused: the symbol active, no retiring run, no candidate (Label is only ever a new
    ''' side), the retired-symbol sentence (Q6).
    ''' </summary>
    <Fact>
    Public Sub AnActiveSymbolIsAnsweredNotRefused()
        Dim label As Long = _scenario.SymbolIdByDocId(_scenario.SampleSolutionId, LabelInLib, True)
        Dim root As JsonElement = Answer(Args("solutionKey", "Sample", "retiredSymbolId", label))
        Dim symbol As JsonElement = root.GetProperty("symbol")
        Assert.True(symbol.GetProperty("isActive").GetBoolean())
        Assert.Equal(JsonValueKind.Null, symbol.GetProperty("retiredInRunId").ValueKind)
        Assert.Equal(0, root.GetProperty("candidates").GetArrayLength())
        Assert.Equal(0, root.GetProperty("runs").GetArrayLength())
        Assert.StartsWith(RetiredSentence, root.GetProperty("note").GetString(), StringComparison.Ordinal)
    End Sub

    ''' <summary>
    ''' (8) A solution with no candidates: total 0, its one run checked 0 = 0, the unfiltered sentence (FR-506).
    ''' </summary>
    <Fact>
    Public Sub ASolutionWithNoCandidatesSaysSo()
        Dim root As JsonElement = Answer(Args("solutionKey", "Other"))
        Assert.Equal(0, root.GetProperty("total").GetInt32())
        Assert.Equal(New Long() {_scenario.OtherRun1}, RunIds(root))
        Dim run As JsonElement = root.GetProperty("runs")(0)
        Assert.True(run.GetProperty("countChecked").GetBoolean())
        Assert.Equal(0, run.GetProperty("candidatesReturned").GetInt32())
        Assert.StartsWith(UnfilteredSentence, root.GetProperty("note").GetString(), StringComparison.Ordinal)
    End Sub

    ''' <summary>
    ''' (9) Both filters (Q5 "both"; analyze G4): run 2 with the retired Describe gives [run 2], one candidate, not checked; run 1 with the
    ''' same id gives [run 1], nothing, not checked, the retired-symbol sentence.
    ''' </summary>
    <Fact>
    Public Sub BothFiltersNarrowToOneRunAndNeverCheck()
        Dim describe As Long = _scenario.SymbolIdByDocId(_scenario.SampleSolutionId, DescribeId, False)
        Dim both As JsonElement = Answer(Args("solutionKey", "Sample", "runId", _scenario.SampleRun2, "retiredSymbolId", describe))
        Assert.Equal(New Long() {_scenario.SampleRun2}, RunIds(both))
        Assert.Equal(1, both.GetProperty("total").GetInt32())
        Assert.False(both.GetProperty("runs")(0).GetProperty("countChecked").GetBoolean())
        Assert.Equal("filtered by retiredSymbolId", both.GetProperty("runs")(0).GetProperty("countNotCheckedReason").GetString())
        Assert.Equal(JsonValueKind.Object, both.GetProperty("symbol").ValueKind)
        Dim empty As JsonElement = Answer(Args("solutionKey", "Sample", "runId", _scenario.SampleRun1, "retiredSymbolId", describe))
        Assert.Equal(New Long() {_scenario.SampleRun1}, RunIds(empty))
        Assert.Equal(0, empty.GetProperty("total").GetInt32())
        Assert.False(empty.GetProperty("runs")(0).GetProperty("countChecked").GetBoolean())
        Assert.StartsWith(RetiredSentence, empty.GetProperty("note").GetString(), StringComparison.Ordinal)
    End Sub

    Private Function Answer(args As Dictionary(Of String, Object)) As JsonElement
        Dim reply As BridgeReply = _scenario.Host.Invoke("rename_candidates", args)
        Assert.False(reply.IsError, reply.Text)
        Return reply.Root()
    End Function

    Private Shared Function Refused(server As BridgeProcess, argumentsJson As String, phrase As String) As ToolReply
        Dim reply As ToolReply = server.CallTool("rename_candidates", argumentsJson)
        Assert.True(reply.IsError, "not refused: " & argumentsJson & " -> " & reply.Text)
        Assert.Contains(phrase, reply.Text, StringComparison.Ordinal)
        Return reply
    End Function

    Private Shared Function RunIds(root As JsonElement) As Long()
        Dim ids As List(Of Long) = New List(Of Long)()
        For Each run As JsonElement In root.GetProperty("runs").EnumerateArray()
            ids.Add(run.GetProperty("runId").GetInt64())
        Next
        Return ids.ToArray()
    End Function

    Private Shared Function RunOf(root As JsonElement, runId As Long) As JsonElement
        For Each run As JsonElement In root.GetProperty("runs").EnumerateArray()
            If run.GetProperty("runId").GetInt64() = runId Then Return run
        Next
        Throw New Xunit.Sdk.XunitException("run " & runId & " is not listed")
    End Function

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

End Class

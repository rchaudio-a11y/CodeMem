' File: B02_PortedReaderTests.vb
' Project: CodeMem.Tests
' Description: The five ported readers on the fixture map: the 056/058/060 shapes, the scope and identity rules, the refusal chain in order and by name, the identity invariant, the description phrases (FR-309..FR-315, FR-348, FR-312).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' Fact (8) reads the BridgeToolDescriptions constants, not a tool, so it is green before any reader exists: a guard on the texts, not
' Red-first, trusted through T024's fire (drop a phrase -> red). B01 (5) ties the texts the server registers to the same constants.
' RED:   2026-09-15 (T017) (1), (1b), (2)-(7) red on NotImplementedException("US1") from BridgeTools - the named Red; (8) green (the
'        constants exist since T014). Build 0 warnings / 0 errors after a VB name clash in the Args helper was renamed.
' GREEN: 2026-09-15 (T023) 9 of 9 after the shared reads (T018), the foundation (T019), the Core reads (T020), the envelopes and readers
'        (T021) and the five tool bodies (T022); (4)'s expected lines were my miscount (5, 13, 17 in MainForm.vb, not 5, 14, 18), corrected.
'        (4) and (7) were then extended so T024's fires (b) and (c) have a fact to redden: references on a type with members (no part_of
'        even there) and symbol_search after a retirement (total 0: a retired row is never listed, FR-314).
' FIRE:  2026-09-15 (T024) (a) the phrase "Containment (part_of) is not a reference and is never included" removed from
'        BridgeToolDescriptions.References -> (8) red naming the phrase; (b) AND s.is_active = 1 dropped from CodeSymbolsRepository.Search
'        -> (7) red (the retired Twins row listed: total expected 0, actual 1); (c) AND e.verb <> 'part_of' dropped from ReadReferences ->
'        (4) red (a part_of occurrence on MainForm); each restored from a byte copy -> green.
'
' 2026-09-17 (feature 005, T010): RED - every fact but (8) red with "schema version 3; this bridge requires 2" once Core moved to 3 at T009
' (the fixture maps are version 3, the pin was 2): the named Red of FR-430. Amended with the pin: (7)'s hand-bump 3 -> 4 for "version above",
' and a version-2 map (Fixtures/Maps/version2.sqlite) added as a second "version below" case with the extractor remedy.
' (1)'s map.schemaVersion literal 2 -> 3 (observed red after the pin moved: expected 2, actual 3).

Imports System.IO
Imports System.Text.Json
Imports CodeMem.Bridging
Imports CodeMem.Core
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Fast facts through the in-process host over one shared extraction (FixtureMapScenario); the production route is B01 (4).
''' </summary>
<Collection("Fixture")>
Public Class B02_PortedReaderTests
    Implements IClassFixture(Of FixtureMapScenario)

    Private Shared ReadOnly ExaminedKinds As String() = New String() {"class", "module", "structure", "interface", "enum", "enum_member", "delegate", "method", "constructor", "property", "field", "event"}
    Private Shared ReadOnly EightVerbs As String() = New String() {"part_of", "calls", "uses", "implements", "extends", "imports", "depends_on", "handles"}

    Private ReadOnly _scenario As FixtureMapScenario

    ''' <summary>
    ''' Receives the shared extraction.
    ''' </summary>
    ''' <param name="scenario">The class fixture.</param>
    Public Sub New(scenario As FixtureMapScenario)
        _scenario = scenario
    End Sub

    ''' <summary>
    ''' (1) solutions has the 056 §1.1 shape: every property present, the identity, one solution with its latest run and the ten counts equal
    ''' to the map's row, dirtyState unknown exactly when commitSha is null.
    ''' </summary>
    <Fact>
    Public Sub SolutionsHasTheMapReadShape()
        Dim reply As BridgeReply = _scenario.Host.Invoke("solutions", Args())
        Assert.False(reply.IsError, reply.Text)
        Dim root As JsonElement = reply.Root()
        Dim map As JsonElement = root.GetProperty("map")
        Assert.Equal(MapQueries.ReadMapGuid(_scenario.Map.Path), map.GetProperty("guid").GetString())
        Assert.Equal(3, map.GetProperty("schemaVersion").GetInt32())
        Assert.Equal(JsonValueKind.String, map.GetProperty("createdUtc").ValueKind)
        Assert.Equal(_scenario.Map.Path, root.GetProperty("mapPath").GetString())
        Assert.Equal(JsonValueKind.String, root.GetProperty("readAtUtc").ValueKind)
        Dim solutions As JsonElement = root.GetProperty("solutions")
        Assert.Equal(1, solutions.GetArrayLength())
        Dim solution As JsonElement = solutions(0)
        AssertHas(solution, "key", "name", "repoRoot", "lastSeenPath", "latestRun")
        Assert.Equal("Sample", solution.GetProperty("key").GetString())
        Dim run As JsonElement = solution.GetProperty("latestRun")
        Assert.NotEqual(JsonValueKind.Null, run.ValueKind)
        AssertHas(run, "runId", "outcome", "sourceDigest", "commitSha", "dirtyState", "schemaVersion", "sdkVersion", "startedUtc", "finishedUtc",
                  "symbolsObserved", "symbolsMatched", "symbolsReactivated", "symbolsNew", "symbolsRetired", "registryActiveBefore", "notesOrphaned",
                  "renameCandidates", "unaccountedObserved", "unaccountedRegistry")
        Dim rows As List(Of RunRow) = MapQueries.ReadRuns(_scenario.Map.Path)
        Dim last As RunRow = rows(rows.Count - 1)
        Assert.Equal(last.Id, run.GetProperty("runId").GetInt64())
        Assert.Equal("completed", run.GetProperty("outcome").GetString())
        Assert.Equal(last.SymbolsObserved, run.GetProperty("symbolsObserved").GetInt32())
        Assert.Equal(last.SymbolsMatched, run.GetProperty("symbolsMatched").GetInt32())
        Assert.Equal(last.SymbolsReactivated, run.GetProperty("symbolsReactivated").GetInt32())
        Assert.Equal(last.SymbolsNew, run.GetProperty("symbolsNew").GetInt32())
        Assert.Equal(last.SymbolsRetired, run.GetProperty("symbolsRetired").GetInt32())
        Assert.Equal(last.RegistryActiveBefore, run.GetProperty("registryActiveBefore").GetInt32())
        Assert.Equal(last.NotesOrphaned, run.GetProperty("notesOrphaned").GetInt32())
        Assert.Equal(last.RenameCandidates, run.GetProperty("renameCandidates").GetInt32())
        Assert.Equal(last.UnaccountedObserved, run.GetProperty("unaccountedObserved").GetInt32())
        Assert.Equal(last.UnaccountedRegistry, run.GetProperty("unaccountedRegistry").GetInt32())
        Dim commitNull As Boolean = run.GetProperty("commitSha").ValueKind = JsonValueKind.Null
        Assert.Equal(commitNull, run.GetProperty("dirtyState").GetString() = "unknown")
    End Sub

    ''' <summary>
    ''' (1b) solutions reports the latest run of any outcome (INC4): on a map whose newest run is a failed row, latestRun is that row.
    ''' </summary>
    <Fact>
    Public Sub SolutionsReportsTheLatestRunOfAnyOutcome()
        Using map As TempMap = New TempMap()
            Using registry As RegistryFixture = New RegistryFixture()
                Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = _scenario.SolutionPath, .DbPath = map.Path}
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim seams As RunSeams = New RunSeams With {.CorruptStagedCounts = Sub(c As RunCounts) c.SymbolsMatched += 1}
                Assert.Equal(ExitCode.ResidualMismatch, ExtractionRun.Execute(options, seams))
                Dim rows As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
                Assert.Equal("failed", rows(rows.Count - 1).Outcome)
                registry.Seed(131373, "Sample", MapQueries.ReadSolutions(map.Path)(0).Id, "active", _scenario.SolutionPath)
                Dim host As BridgeHost = New BridgeHost(BridgeHost.WriteConfig(map.Path, registry.Path, Nothing, False, False))
                Dim reply As BridgeReply = host.Invoke("solutions", Args())
                Assert.False(reply.IsError, reply.Text)
                Dim run As JsonElement = reply.Root().GetProperty("solutions")(0).GetProperty("latestRun")
                Assert.Equal("failed", run.GetProperty("outcome").GetString())
                Assert.Equal(rows(rows.Count - 1).Id, run.GetProperty("runId").GetInt64())
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (2) symbol_search by key and by project return the same rows and total; by project the registry counts are numbers, by key null;
    ''' projectSymbolId narrows the rows and never the total.
    ''' </summary>
    <Fact>
    Public Sub SymbolSearchByKeyAndByProjectAgree()
        Dim byKey As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "Widget"))
        Dim byProject As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("projectId", 131373L, "name", "Widget"))
        Assert.False(byKey.IsError, byKey.Text)
        Assert.False(byProject.IsError, byProject.Text)
        Assert.Equal(Canonical(byKey.Root().GetProperty("symbols")), Canonical(byProject.Root().GetProperty("symbols")))
        Assert.True(byKey.Root().GetProperty("symbols").GetArrayLength() > 0, "no Widget rows")
        Assert.Equal(byKey.Root().GetProperty("total").GetInt32(), byProject.Root().GetProperty("total").GetInt32())
        Assert.False(byKey.Root().GetProperty("truncated").GetBoolean())
        Assert.False(byProject.Root().GetProperty("truncated").GetBoolean())
        Dim projectScope As JsonElement = byProject.Root().GetProperty("scope")
        Assert.Equal("projectId", projectScope.GetProperty("by").GetString())
        For Each name As String In New String() {"activeRegistryRows", "unboundRegistryRows", "inactiveRegistryRows"}
            Assert.Equal(JsonValueKind.Number, projectScope.GetProperty(name).ValueKind)
        Next
        Assert.Equal(JsonValueKind.Array, projectScope.GetProperty("danglingSolutionIds").ValueKind)
        Assert.False(projectScope.GetProperty("hadNothingToSearch").GetBoolean())
        Dim keyScope As JsonElement = byKey.Root().GetProperty("scope")
        Assert.Equal("solutionKey", keyScope.GetProperty("by").GetString())
        For Each name As String In New String() {"activeRegistryRows", "unboundRegistryRows", "inactiveRegistryRows", "danglingSolutionIds"}
            Assert.Equal(JsonValueKind.Null, keyScope.GetProperty(name).ValueKind)
        Next
        Dim libProject As Long = _scenario.SymbolId("Project:Sample.Lib")
        Dim narrowed As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "Widget", "projectSymbolId", libProject))
        Assert.False(narrowed.IsError, narrowed.Text)
        Assert.Equal(byKey.Root().GetProperty("total").GetInt32(), narrowed.Root().GetProperty("total").GetInt32())
        Assert.True(narrowed.Root().GetProperty("symbols").GetArrayLength() < byKey.Root().GetProperty("symbols").GetArrayLength(), "the project filter narrowed nothing")
        For Each row As JsonElement In narrowed.Root().GetProperty("symbols").EnumerateArray()
            Assert.Equal(libProject, row.GetProperty("project").GetProperty("id").GetInt64())
        Next
    End Sub

    ''' <summary>
    ''' (3) symbol_detail of the partial MainForm: two parts with exactly one flagged, all eight verbs on both sides, the Designer base
    ''' class as an external extends target with its doc-comment id.
    ''' </summary>
    <Fact>
    Public Sub SymbolDetailShowsPartsAndEightVerbs()
        Dim reply As BridgeReply = _scenario.Host.Invoke("symbol_detail", Args("solutionKey", "Sample", "symbolId", _scenario.SymbolId("T:Sample.MainForm")))
        Assert.False(reply.IsError, reply.Text)
        Dim root As JsonElement = reply.Root()
        Dim parts As JsonElement = root.GetProperty("parts")
        Assert.Equal(2, parts.GetArrayLength())
        Dim flagged As Integer = 0
        For Each part As JsonElement In parts.EnumerateArray()
            AssertHas(part, "path", "line", "column", "containsDeclaringLocation")
            If part.GetProperty("containsDeclaringLocation").GetBoolean() Then flagged += 1
        Next
        Assert.Equal(1, flagged)
        For Each side As String In New String() {"outbound", "inbound"}
            AssertHas(root.GetProperty(side), EightVerbs)
        Next
        Dim extendsForm As Boolean = False
        For Each edge As JsonElement In root.GetProperty("outbound").GetProperty("extends").EnumerateArray()
            If edge.GetProperty("targetDocCommentId").GetString() = "T:System.Windows.Forms.Form" Then
                extendsForm = True
                Assert.Equal(JsonValueKind.Null, edge.GetProperty("targetSymbolId").ValueKind)
                Assert.True(edge.GetProperty("external").GetBoolean())
            End If
        Next
        Assert.True(extendsForm, "no outbound extends to System.Windows.Forms.Form")
    End Sub

    ''' <summary>
    ''' (4) references of MainForm's Timer1: the three bare-name occurrences 003 recorded at their lines, no part_of, count equal to the list.
    ''' </summary>
    <Fact>
    Public Sub ReferencesAreExactIdentityWithoutContainment()
        Dim reply As BridgeReply = _scenario.Host.Invoke("references", Args("solutionKey", "Sample", "symbolId", _scenario.SymbolId("P:Sample.MainForm.Timer1")))
        Assert.False(reply.IsError, reply.Text)
        Dim root As JsonElement = reply.Root()
        Dim occurrences As JsonElement = root.GetProperty("occurrences")
        Dim lines As List(Of Integer) = New List(Of Integer)()
        For Each occurrence As JsonElement In occurrences.EnumerateArray()
            Assert.NotEqual("part_of", occurrence.GetProperty("verb").GetString())
            If occurrence.GetProperty("path").GetString() = "Sample.App/MainForm.vb" Then lines.Add(occurrence.GetProperty("line").GetInt32())
        Next
        For Each expected As Integer In New Integer() {5, 13, 17}
            Assert.Contains(expected, lines)
        Next
        Assert.Equal(occurrences.GetArrayLength(), root.GetProperty("count").GetInt32())
        Dim typeReferences As BridgeReply = _scenario.Host.Invoke("references", Args("solutionKey", "Sample", "symbolId", _scenario.SymbolId("T:Sample.MainForm")))
        Assert.False(typeReferences.IsError, typeReferences.Text)
        For Each occurrence As JsonElement In typeReferences.Root().GetProperty("occurrences").EnumerateArray()
            Assert.NotEqual("part_of", occurrence.GetProperty("verb").GetString())
        Next
    End Sub

    ''' <summary>
    ''' (5) orphans has the 060 shape: filters, notExamined, byProject for both projects, byKind with the twelve examined kinds, total equal to
    ''' the list; an unexamined kind and a non-project row are refused by name.
    ''' </summary>
    <Fact>
    Public Sub OrphansHaveTheSixtyShape()
        Dim reply As BridgeReply = _scenario.Host.Invoke("orphans", Args("solutionKey", "Sample"))
        Assert.False(reply.IsError, reply.Text)
        Dim root As JsonElement = reply.Root()
        AssertHas(root.GetProperty("filters"), "kind", "projectSymbolId")
        Assert.Equal(New String() {"namespace", "project"}, Strings(root.GetProperty("notExamined")))
        Dim projects As List(Of String) = New List(Of String)()
        For Each project As JsonElement In root.GetProperty("byProject").EnumerateArray()
            AssertHas(project, "solutionId", "projectSymbolId", "name", "count")
            projects.Add(project.GetProperty("name").GetString())
        Next
        Assert.Contains("Sample.Lib", projects)
        Assert.Contains("Sample.App", projects)
        AssertHas(root.GetProperty("byKind"), ExaminedKinds)
        Assert.Equal(12, CountProperties(root.GetProperty("byKind")))
        Assert.Equal(root.GetProperty("orphans").GetArrayLength(), root.GetProperty("total").GetInt32())
        Dim unexamined As BridgeReply = _scenario.Host.Invoke("orphans", Args("solutionKey", "Sample", "kind", "project"))
        Assert.True(unexamined.IsError, "kind project was not refused")
        Assert.Contains("does not examine", unexamined.Text)
        Dim notProject As BridgeReply = _scenario.Host.Invoke("orphans", Args("solutionKey", "Sample", "projectSymbolId", _scenario.SymbolId("T:Sample.Consumer")))
        Assert.True(notProject.IsError, "a class id as projectSymbolId was not refused")
        Assert.Contains("not a project row", notProject.Text)
    End Sub

    ''' <summary>
    ''' (6) Every occurrence symbol_detail and references return carries a mapped id or the external marking with a doc-comment id (FR-348).
    ''' </summary>
    <Fact>
    Public Sub EveryOccurrenceCarriesAnIdOrIsExternal()
        Dim detail As BridgeReply = _scenario.Host.Invoke("symbol_detail", Args("solutionKey", "Sample", "symbolId", _scenario.SymbolId("T:Sample.MainForm")))
        Assert.False(detail.IsError, detail.Text)
        Dim checked As Integer = 0
        For Each side As String In New String() {"outbound", "inbound"}
            For Each verb As String In EightVerbs
                For Each edge As JsonElement In detail.Root().GetProperty(side).GetProperty(verb).EnumerateArray()
                    OccurrenceAssertions.AssertIdentity(edge)
                    checked += 1
                Next
            Next
        Next
        Dim references As BridgeReply = _scenario.Host.Invoke("references", Args("solutionKey", "Sample", "symbolId", _scenario.SymbolId("P:Sample.MainForm.Timer1")))
        Assert.False(references.IsError, references.Text)
        For Each occurrence As JsonElement In references.Root().GetProperty("occurrences").EnumerateArray()
            OccurrenceAssertions.AssertIdentity(occurrence)
            checked += 1
        Next
        Assert.True(checked > 0, "no occurrence was checked: the scan is vacuous")
    End Sub

    ''' <summary>
    ''' (7) The refusal chain in order (arguments before configuration before store before map before question) and by name, each case's
    ''' distinguishing phrase from contracts/tools.md §6; a projectId with no registry row is not an error.
    ''' </summary>
    <Fact>
    Public Sub RefusalsComeInOrderAndByName()
        Dim absentConfig As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "absent-config-" & Guid.NewGuid().ToString("N") & ".json")
        Dim noConfig As BridgeHost = New BridgeHost(absentConfig)
        Expect("no scope before configuration", noConfig.Invoke("symbol_search", Args("name", "x")), "exactly one of projectId")
        Expect("configuration absent", noConfig.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "x")), "not configured")

        Dim absentMap As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "absent-" & Guid.NewGuid().ToString("N") & ".sqlite")
        Expect("map absent", New BridgeHost(BridgeHost.WriteConfig(absentMap, _scenario.Registry.Path, Nothing, False, False)).Invoke("solutions", Args()), "No CodeMem map exists")
        Assert.False(File.Exists(absentMap), "the refusal created the map")

        Using foreign As TempMap = New TempMap()
            MapQueries.CreateForeignDatabase(foreign.Path)
            Expect("not a map", New BridgeHost(BridgeHost.WriteConfig(foreign.Path, _scenario.Registry.Path, Nothing, False, False)).Invoke("solutions", Args()), "not a CodeMem map")
        End Using
        Using v1 As TempMap = New TempMap()
            V1MapFixture.CopyToTemp(v1)
            Expect("version below", New BridgeHost(BridgeHost.WriteConfig(v1.Path, _scenario.Registry.Path, Nothing, False, False)).Invoke("solutions", Args()), "upgrades the map in place")
        End Using
        Using v2 As TempMap = New TempMap()
            V2MapFixture.CopyToTemp(v2)
            Expect("version below (a 004-era map)", New BridgeHost(BridgeHost.WriteConfig(v2.Path, _scenario.Registry.Path, Nothing, False, False)).Invoke("solutions", Args()), "upgrades the map in place")
        End Using
        Using newer As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _scenario.SolutionPath, .DbPath = newer.Path}, Nothing))
            MapQueries.SetSchemaVersion(newer.Path, 4)
            Expect("version above", New BridgeHost(BridgeHost.WriteConfig(newer.Path, _scenario.Registry.Path, Nothing, False, False)).Invoke("solutions", Args()), "newer contract")
        End Using
        Using noTable As RegistryFixture = RegistryFixture.WithoutTable()
            Expect("registry absent", New BridgeHost(BridgeHost.WriteConfig(_scenario.Map.Path, noTable.Path, Nothing, False, False)).Invoke("symbol_search", Args("projectId", 131373L, "name", "x")), "no code_map_solutions table")
        End Using

        Dim host As BridgeHost = _scenario.Host
        Expect("both scopes", host.Invoke("symbol_search", Args("projectId", 131373L, "solutionKey", "Sample", "name", "x")), "Both")
        Expect("no filter", host.Invoke("symbol_search", Args("solutionKey", "Sample")), "name, a kind, or both")
        Dim kindUnknown As BridgeReply = host.Invoke("symbol_search", Args("solutionKey", "Sample", "kind", "widget"))
        Expect("kind unknown", kindUnknown, "not a symbol kind")
        Assert.Contains("enum_member", kindUnknown.Text)
        Assert.Contains("namespace", kindUnknown.Text)
        Expect("key unknown", host.Invoke("symbol_search", Args("solutionKey", "Nope", "name", "x")), "holds no solution with key")
        Expect("symbol not found", host.Invoke("symbol_detail", Args("solutionKey", "Sample", "symbolId", 999999L)), "holds no symbol with id")

        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Using registry As RegistryFixture = New RegistryFixture()
                    Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path, .SolutionKey = "Sample"}
                    Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                    File.Delete(Path.Combine(copy.Directory, "Sample.Lib", "Twins.vb"))
                    Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                    Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                    Dim retired As SymbolRow = MapQueries.ReadSymbols(map.Path, solutionId).Find(Function(s As SymbolRow) s.DocCommentId = "T:Sample.Widgets.Twins" AndAlso Not s.IsActive)
                    Assert.True(retired IsNot Nothing, "Twins was not retired")
                    registry.Seed(131373, "Sample", solutionId, "active", copy.SolutionPath)
                    Dim retiredHost As BridgeHost = New BridgeHost(BridgeHost.WriteConfig(map.Path, registry.Path, Nothing, False, False))
                    Expect("symbol retired", retiredHost.Invoke("symbol_detail", Args("solutionKey", "Sample", "symbolId", retired.Id)), "retired")
                    Dim retiredSearch As BridgeReply = retiredHost.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "Twins", "kind", "class"))
                    Assert.False(retiredSearch.IsError, retiredSearch.Text)
                    Assert.Equal(0, retiredSearch.Root().GetProperty("total").GetInt32())
                End Using
            End Using
        End Using

        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Using registry As RegistryFixture = New RegistryFixture()
                    Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _scenario.SolutionPath, .DbPath = map.Path, .SolutionKey = "Sample"}, Nothing))
                    Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path, .SolutionKey = "Other"}, Nothing))
                    Dim solutions As List(Of SolutionRow) = MapQueries.ReadSolutions(map.Path)
                    Dim sample As SolutionRow = solutions.Find(Function(s As SolutionRow) s.Key = "Sample")
                    Dim other As SolutionRow = solutions.Find(Function(s As SolutionRow) s.Key = "Other")
                    Dim foreignId As Long = MapQueries.ReadSymbols(map.Path, other.Id).Find(Function(s As SymbolRow) s.DocCommentId = "T:Sample.Consumer" AndAlso s.IsActive).Id
                    registry.Seed(131373, "Sample", sample.Id, "active", _scenario.SolutionPath)
                    Expect("symbol out of scope", New BridgeHost(BridgeHost.WriteConfig(map.Path, registry.Path, Nothing, False, False)).Invoke("symbol_detail", Args("solutionKey", "Sample", "symbolId", foreignId)), "not in the requested scope")
                End Using
            End Using
        End Using

        Dim nothingToSearch As BridgeReply = host.Invoke("symbol_search", Args("projectId", 424242L, "name", "x"))
        Assert.False(nothingToSearch.IsError, nothingToSearch.Text)
        Dim scope As JsonElement = nothingToSearch.Root().GetProperty("scope")
        Assert.True(scope.GetProperty("hadNothingToSearch").GetBoolean())
        Assert.Contains("424242", scope.GetProperty("reason").GetString())
        Assert.Equal(0, nothingToSearch.Root().GetProperty("total").GetInt32())
    End Sub

    ''' <summary>
    ''' (8) Every caveat phrase contracts/tools.md §2 marks in bold is present in the registered description of its tool (FR-312).
    ''' </summary>
    <Fact>
    Public Sub DescriptionsCarryTheCaveats()
        Dim phrases As Dictionary(Of String, String()) = New Dictionary(Of String, String())(StringComparer.Ordinal) From {
            {BridgeToolDescriptions.Solutions, New String() {"unknown when there is no commit"}},
            {BridgeToolDescriptions.SymbolSearch, New String() {"at most 200", "narrow the filter rather than page", "A file linked into several projects declares one symbol per project; such twins are presented as one declaration compiled into N projects, carrying every project's symbol id."}},
            {BridgeToolDescriptions.SymbolDetail, New String() {"eight verbs", "marked external", "outside that scope", "does not exist", "retired"}},
            {BridgeToolDescriptions.References, New String() {"Containment (part_of) is not a reference and is never included", "Does not show AddHandler … AddressOf wiring sites", "A field or property read or written by its bare name, and a method passed by a bare AddressOf, are recorded as calls occurrences and appear here (CodeMem 003). A bare-name use whose target lies outside the map (framework, package) is not recorded.", "Takes a mapped symbol id only"}},
            {BridgeToolDescriptions.Orphans, New String() {"used by its bare name", "no namespace rows", "unreferenced by construction", "not a verdict that it is dead", "Main", "reflection", "Overrides", "InitializeComponent", "prevent instantiation", "AddressOf", "outside the solution", "generated outside the solution tree", "referenced only by a sibling", "implemented but never called"}},
            {BridgeToolDescriptions.TypeUsages, New String() {"calls to each of its constructors", "from inside", "total", "fromOutside"}},
            {BridgeToolDescriptions.MapStatus, New String() {"one verdict by name", "current", "behind", "dirty", "no_git", "map_missing_solution", "diverged"}},
            {BridgeToolDescriptions.Extract, New String() {"Gated"}}}
        Dim missing As List(Of String) = New List(Of String)()
        For Each pair As KeyValuePair(Of String, String()) In phrases
            For Each phrase As String In pair.Value
                If Not pair.Key.Contains(phrase, StringComparison.Ordinal) Then missing.Add(phrase)
            Next
        Next
        Assert.True(missing.Count = 0, "phrases missing from the descriptions: " & String.Join(" | ", missing))
    End Sub

    Private Shared Sub Expect(caseName As String, reply As BridgeReply, phrase As String)
        Assert.True(reply.IsError, caseName & ": not refused; got " & reply.Text)
        Assert.True(reply.Text.Contains(phrase, StringComparison.Ordinal), caseName & ": expected the phrase '" & phrase & "' in: " & reply.Text)
    End Sub

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

    Private Shared Sub AssertHas(element As JsonElement, ParamArray names As String())
        Dim value As JsonElement
        For Each name As String In names
            Assert.True(element.TryGetProperty(name, value), "property missing: " & name & " in " & element.GetRawText())
        Next
    End Sub

    Private Shared Function Strings(array As JsonElement) As String()
        Dim result As List(Of String) = New List(Of String)()
        For Each item As JsonElement In array.EnumerateArray()
            result.Add(item.GetString())
        Next
        Return result.ToArray()
    End Function

    Private Shared Function CountProperties(element As JsonElement) As Integer
        Dim count As Integer = 0
        For Each unused As JsonProperty In element.EnumerateObject()
            count += 1
        Next
        Return count
    End Function

    Private Shared Function Canonical(symbols As JsonElement) As List(Of String)
        Dim lines As List(Of String) = New List(Of String)()
        For Each row As JsonElement In symbols.EnumerateArray()
            lines.Add(row.GetProperty("id").GetInt64() & "|" & row.GetProperty("docCommentId").GetString() & "|" & row.GetProperty("kind").GetString() & "|" & row.GetProperty("name").GetString() &
                      "|" & row.GetProperty("container").GetRawText() & "|" & row.GetProperty("project").GetRawText() & "|" & row.GetProperty("path").GetString() & "|" & row.GetProperty("line").GetInt32())
        Next
        Return lines
    End Function

End Class

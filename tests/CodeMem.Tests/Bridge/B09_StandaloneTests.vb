' File: B09_StandaloneTests.vb
' Project: CodeMem.Tests
' Description: US1 (feature 005): the bridge answers from the map and nothing else - every tool with no store anywhere; projectId and storePath refused by name; the descriptions speak of no registry; the envelopes carry no registry field (FR-403, FR-405-FR-409, FR-418, FR-433).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' (1) and (2) run through the executable over stdio (CON3); (3) and (5) through the in-process host. (6), the vocabulary count, arrives at T021.
' RED:   2026-09-17 (T012) compile failure, the named Red: BridgeHost.WriteConfig(mapPath, extractorPath, enabled, onGreenBuild) does not exist
'        (BC30455 - the four-argument call binds to nothing while storePath is a parameter); the facts' other subjects (ProjectIdRemoved,
'        ConfigKeyRetired, the trimmed envelopes, the revised descriptions) are behind the same build. The slices land in T013-T017.
' GREEN: 2026-09-17 (T014-T016) 5 of 5 on the first build: (2) through the executable proves the SDK injects RequestContext into the bound
'        methods (research R68's primary mechanism; the fallback was not needed), the library taking the raw dictionary (BridgeToolBindings).
' FIRE:  2026-09-17 (T018) (2): ScopeResolver.RefuseProjectId returned before reading the dictionary -> red ("solutions: projectId was not
'        refused; got {...map...}"); reverted -> green.

Imports System.IO
Imports System.Text.Json
Imports CodeMem.Bridging
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The one-database facts over a fixture map: no store file exists anywhere the tests look, and no configuration names one.
''' </summary>
<Collection("Fixture")>
Public Class B09_StandaloneTests
    Implements IClassFixture(Of FixtureMapScenario)

    Private Shared ReadOnly ForbiddenPhrases As String() = New String() {"projectId", "registry", "code_map_solutions", "MemOS project"}

    Private ReadOnly _scenario As FixtureMapScenario

    ''' <summary>
    ''' Receives the shared extraction.
    ''' </summary>
    ''' <param name="scenario">The class fixture.</param>
    Public Sub New(scenario As FixtureMapScenario)
        _scenario = scenario
    End Sub

    ''' <summary>
    ''' (1) Every read tool and map_status answers through the executable against a map whose configuration names no store, with no store
    ''' file on disk (FR-433).
    ''' </summary>
    <Fact>
    Public Sub EveryToolAnswersWithNoStoreAnywhere()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _scenario.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim consumerId As Long = SymbolId(map.Path, solutionId, "T:Sample.Consumer")
            Dim config As String = BridgeHost.WriteConfig(map.Path, Nothing, False, False)
            Assert.DoesNotContain("storePath", File.ReadAllText(config), StringComparison.Ordinal)
            Dim called As Integer = 0
            Using server As BridgeProcess = BridgeProcess.Serve(config)
                server.Initialize()
                For Each name As String In server.ListTools()
                    If name = "extract" Then Continue For
                    Dim reply As ToolReply = server.CallTool(name, ArgumentsFor(name, consumerId))
                    Assert.False(reply.IsError, name & ": " & reply.Text)
                    called += 1
                Next
                Assert.True(called >= 7, "fewer than seven read tools listed: the scan is vacuous")
                Assert.Equal(0, server.Close())
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (2) A call carrying projectId, beside its ordinary arguments, is refused naming the argument on every one of the eight tools, and
    ''' still when the configuration file does not exist (arguments before configuration) (FR-406).
    ''' </summary>
    <Fact>
    Public Sub ProjectIdIsRefusedByNameOnEveryTool()
        Dim consumerId As Long = _scenario.SymbolId("T:Sample.Consumer")
        Using server As BridgeProcess = BridgeProcess.Serve(_scenario.ConfigPath)
            server.Initialize()
            Dim names As List(Of String) = server.ListTools()
            Assert.Equal(8, names.Count)
            For Each name As String In names
                Dim reply As ToolReply = server.CallTool(name, WithProjectId(ArgumentsFor(name, consumerId)))
                Assert.True(reply.IsError, name & ": projectId was not refused; got " & reply.Text)
                Assert.Contains("projectId is not an argument", reply.Text, StringComparison.Ordinal)
            Next
            Assert.Equal(0, server.Close())
        End Using
        Dim absentConfig As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "absent-config-" & Guid.NewGuid().ToString("N") & ".json")
        Using server As BridgeProcess = BridgeProcess.Serve(absentConfig)
            server.Initialize()
            Dim reply As ToolReply = server.CallTool("solutions", WithProjectId("{}"))
            Assert.True(reply.IsError, "projectId with no configuration was not refused; got " & reply.Text)
            Assert.Contains("projectId is not an argument", reply.Text, StringComparison.Ordinal)
            Assert.DoesNotContain("not configured", reply.Text, StringComparison.Ordinal)
            Assert.Equal(0, server.Close())
        End Using
    End Sub

    ''' <summary>
    ''' (3) A configuration file carrying storePath is refused naming the key, on a read tool and on extract, before anything is opened (FR-403).
    ''' </summary>
    <Fact>
    Public Sub StorePathInTheConfigIsRefusedByName()
        Dim dir As String = Path.Combine(Path.GetTempPath(), "codemem-tests")
        Directory.CreateDirectory(dir)
        Dim file As String = Path.Combine(dir, "bridge-config-store-" & Guid.NewGuid().ToString("N") & ".json")
        Dim document As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal) From {
            {"mapPath", _scenario.Map.Path},
            {"storePath", "x"},
            {"extract", New Dictionary(Of String, Object)(StringComparer.Ordinal) From {{"enabled", True}, {"onGreenBuild", False}}}}
        IO.File.WriteAllText(file, JsonSerializer.Serialize(document))
        Dim host As BridgeHost = New BridgeHost(file)
        Dim read As BridgeReply = host.Invoke("solutions", Args())
        Assert.True(read.IsError, "solutions did not refuse the storePath key; got " & read.Text)
        Assert.Contains("no longer a key", read.Text, StringComparison.Ordinal)
        Assert.Contains("storePath", read.Text, StringComparison.Ordinal)
        Dim extract As BridgeReply = host.Invoke("extract", Args("solutionKey", "Sample"))
        Assert.True(extract.IsError, "extract did not refuse the storePath key; got " & extract.Text)
        Assert.Contains("no longer a key", extract.Text, StringComparison.Ordinal)
        Assert.Equal(0, host.Launcher.Requests.Count)
    End Sub

    ''' <summary>
    ''' (4) The descriptions the server registers equal the constants, none speaks of projectId, a registry, code_map_solutions or a MemOS
    ''' project, and every 004 caveat phrase is still present (FR-407, FR-312).
    ''' </summary>
    <Fact>
    Public Sub TheDescriptionsSpeakOfNoRegistry()
        Using server As BridgeProcess = BridgeProcess.Serve(_scenario.ConfigPath)
            server.Initialize()
            Dim descriptions As Dictionary(Of String, String) = server.ListToolDescriptions()
            Assert.Equal(8, descriptions.Count)
            Dim offenders As List(Of String) = New List(Of String)()
            For Each pair As KeyValuePair(Of String, String) In descriptions
                Assert.Equal(BridgeTools.RegisteredToolDescriptions(pair.Key), pair.Value)
                For Each phrase As String In ForbiddenPhrases
                    If pair.Value.Contains(phrase, StringComparison.Ordinal) Then offenders.Add(pair.Key & ": " & phrase)
                Next
            Next
            Assert.True(offenders.Count = 0, "registry vocabulary in a description: " & String.Join(" | ", offenders))
            Assert.Equal(0, server.Close())
        End Using
        Dim caveats As Dictionary(Of String, String()) = New Dictionary(Of String, String())(StringComparer.Ordinal) From {
            {BridgeToolDescriptions.Solutions, New String() {"unknown when there is no commit"}},
            {BridgeToolDescriptions.SymbolSearch, New String() {"at most 200", "narrow the filter rather than page", "as one declaration compiled into N projects, carrying every project's symbol id"}},
            {BridgeToolDescriptions.SymbolDetail, New String() {"eight verbs", "marked external", "does not exist", "retired"}},
            {BridgeToolDescriptions.References, New String() {"Containment (part_of) is not a reference and is never included", "Does not show AddHandler … AddressOf wiring sites", "Takes a mapped symbol id only"}},
            {BridgeToolDescriptions.Orphans, New String() {"used by its bare name", "no namespace rows", "unreferenced by construction", "not a verdict that it is dead", "referenced only by a sibling", "implemented but never called"}},
            {BridgeToolDescriptions.TypeUsages, New String() {"calls to each of its constructors", "from inside", "fromOutside"}},
            {BridgeToolDescriptions.MapStatus, New String() {"one verdict by name", "current", "behind", "dirty", "no_git", "diverged", "not_in_map"}},
            {BridgeToolDescriptions.Extract, New String() {"Gated", "solutionPath", "not in the map"}}}
        Dim missing As List(Of String) = New List(Of String)()
        For Each pair As KeyValuePair(Of String, String()) In caveats
            For Each phrase As String In pair.Value
                If Not pair.Key.Contains(phrase, StringComparison.Ordinal) Then missing.Add(phrase)
            Next
        Next
        Assert.True(missing.Count = 0, "phrases missing from the descriptions: " & String.Join(" | ", missing))
    End Sub

    ''' <summary>
    ''' (5) map_status carries no storePath, bound, unbound or inactive, its entries a solutionId and no projectId or codememSolutionId;
    ''' symbol_search's scope carries by, solutionKey and solutions and nothing else (FR-405, FR-418).
    ''' </summary>
    <Fact>
    Public Sub TheEnvelopesCarryNoRegistryFields()
        Dim status As BridgeReply = _scenario.Host.Invoke("map_status", Args())
        Assert.False(status.IsError, status.Text)
        Dim root As JsonElement = status.Root()
        For Each name As String In New String() {"storePath", "bound", "unbound", "inactive"}
            Assert.False(HasProperty(root, name), "map_status still carries " & name)
        Next
        Dim entries As JsonElement = root.GetProperty("entries")
        Assert.Equal(1, entries.GetArrayLength())
        Dim entry As JsonElement = entries(0)
        Assert.True(HasProperty(entry, "solutionId"), "entry without solutionId")
        Assert.Equal("Sample", entry.GetProperty("solutionKey").GetString())
        For Each name As String In New String() {"projectId", "codememSolutionId"}
            Assert.False(HasProperty(entry, name), "entry still carries " & name)
        Next
        Assert.True(HasProperty(root, "notInMap"), "map_status without notInMap")

        Dim search As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "Widget"))
        Assert.False(search.IsError, search.Text)
        Dim scope As JsonElement = search.Root().GetProperty("scope")
        Dim names As List(Of String) = New List(Of String)()
        For Each p As JsonProperty In scope.EnumerateObject()
            names.Add(p.Name)
        Next
        names.Sort(StringComparer.Ordinal)
        Assert.Equal(New String() {"by", "solutionKey", "solutions"}, names.ToArray())
        Assert.Equal("solutionKey", scope.GetProperty("by").GetString())
    End Sub

    Private Shared Function ArgumentsFor(name As String, consumerId As Long) As String
        Select Case name
            Case "solutions", "map_status"
                Return "{}"
            Case "symbol_search"
                Return "{""solutionKey"":""Sample"",""name"":""Widget""}"
            Case "orphans"
                Return "{""solutionKey"":""Sample""}"
            Case "extract"
                Return "{""solutionKey"":""Sample""}"
            Case Else
                Return "{""solutionKey"":""Sample"",""symbolId"":" & consumerId & "}"
        End Select
    End Function

    Private Shared Function WithProjectId(argumentsJson As String) As String
        If argumentsJson = "{}" Then Return "{""projectId"":132040}"
        Return "{""projectId"":132040," & argumentsJson.Substring(1)
    End Function

    Private Shared Function SymbolId(mapPath As String, solutionId As Long, docCommentId As String) As Long
        Dim row As SymbolRow = MapQueries.ReadSymbols(mapPath, solutionId).Find(Function(s As SymbolRow) s.DocCommentId = docCommentId AndAlso s.IsActive)
        Assert.NotNull(row)
        Return row.Id
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

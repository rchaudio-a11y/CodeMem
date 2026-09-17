' File: BridgeTools.vb
' Project: CodeMem.Bridging
' Description: The eight tool methods the bindings and the in-process host call; each takes the call's raw arguments and its typed ones and returns the envelope or a refusal as a CallToolResult (005 contracts/tools.md).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-15 (T011): the skeleton behind B01's Red - the signatures of contracts/tools.md §1, each throwing until its story lands; the
' registered names and descriptions grow as each story registers its tool.
' 2026-09-15 (T022): the five ported readers, every call in one shape (CON4, INC2): argument refusals -> configuration -> store (only for
' projectId) -> the map, opened once with one read transaction -> scope -> the seam -> the reader -> serialise -> EndRead -> answer.
' 2026-09-15 (T029): type_usages registered, the same shape. (T033): map_status registered; the registry read at the store stage, the
' map facts inside the read transaction, the repositories asked after them. (T043): extract registered - origin Tool, the door's result as
' the text block, IsError when refused, failed or timed out.
' 2026-09-17 (feature 005, T014): projectId and the store stage are gone (FR-405, FR-406). Every method's first parameter is the call's raw
' argument dictionary, which ScopeResolver.RefuseProjectId reads (research R68: the protocol layer ignores an argument no parameter names);
' BridgeToolBindings hands it the SDK's request context, BridgeHost a dictionary of its own. The shape is now arguments -> configuration ->
' the map -> scope -> the seam -> the reader -> serialise -> EndRead -> answer.

Imports System.Text.Json
Imports CodeMem.Core
Imports Microsoft.Data.Sqlite
Imports ModelContextProtocol.Protocol

''' <summary>
''' One instance per host or server; the configuration is read on every call, never here; the extract door is built once over the launcher.
''' </summary>
Public Class BridgeTools

    Private Shared ReadOnly Names As String() = New String() {"solutions", "symbol_search", "symbol_detail", "references", "orphans", "type_usages", "map_status", "extract"}
    Private Shared ReadOnly Descriptions As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal) From {
        {"solutions", BridgeToolDescriptions.Solutions},
        {"symbol_search", BridgeToolDescriptions.SymbolSearch},
        {"symbol_detail", BridgeToolDescriptions.SymbolDetail},
        {"references", BridgeToolDescriptions.References},
        {"orphans", BridgeToolDescriptions.Orphans},
        {"type_usages", BridgeToolDescriptions.TypeUsages},
        {"map_status", BridgeToolDescriptions.MapStatus},
        {"extract", BridgeToolDescriptions.Extract}}

    Private ReadOnly _configPath As String
    Private ReadOnly _door As ExtractDoor
    Private ReadOnly _seams As ReadSeams

    ''' <summary>
    ''' Binds the tools to a configuration file and, for the extract verb, a launcher.
    ''' </summary>
    ''' <param name="configPath">The configuration file read on every call, or Nothing for the file beside the executable.</param>
    ''' <param name="launcher">The extractor launcher the extract door runs the child through (the seam, research R54).</param>
    ''' <param name="seams">Test-only read seams, or Nothing.</param>
    Public Sub New(configPath As String, launcher As IExtractorLauncher, Optional seams As ReadSeams = Nothing)
        _configPath = configPath
        _door = New ExtractDoor(configPath, launcher)
        _seams = seams
    End Sub

    ''' <summary>The names registered so far, in registration order; the server lists exactly these.</summary>
    ''' <returns>A copy of the names.</returns>
    Public Shared ReadOnly Property RegisteredToolNames As String()
        Get
            Return CType(Names.Clone(), String())
        End Get
    End Property

    ''' <summary>The registered description of each name (contracts/tools.md §2).</summary>
    ''' <returns>Name to description.</returns>
    Public Shared ReadOnly Property RegisteredToolDescriptions As IReadOnlyDictionary(Of String, String)
        Get
            Return Descriptions
        End Get
    End Property

    ''' <summary>
    ''' Every solution with its latest run of any outcome (056 §1.1).
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function Solutions(rawArguments As IReadOnlyDictionary(Of String, JsonElement)) As CallToolResult
        Return RunRead(Of SolutionsEnvelope)(
            Sub()
                ScopeResolver.RefuseProjectId(rawArguments)
            End Sub,
            Function(config As BridgeConfig, map As MapDatabase)
                Seam()
                Return SolutionsReader.Read(config, map)
            End Function)
    End Function

    ''' <summary>
    ''' Active symbols by name and/or kind in one solution, twins folded (058 §3.2, FR-340).
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <param name="name">The name filter, or Nothing.</param>
    ''' <param name="kind">The kind filter, or Nothing.</param>
    ''' <param name="projectSymbolId">A project row to narrow to, or Nothing.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function SymbolSearch(rawArguments As IReadOnlyDictionary(Of String, JsonElement), Optional solutionKey As String = Nothing, Optional name As String = Nothing, Optional kind As String = Nothing, Optional projectSymbolId As Long? = Nothing) As CallToolResult
        Dim key As String = Normalise(solutionKey)
        Dim nameFilter As String = Normalise(name)
        Dim kindFilter As String = Normalise(kind)
        Return RunRead(Of SymbolSearchEnvelope)(
            Sub()
                ScopeResolver.ValidateArguments(rawArguments, key)
                SymbolSearchReader.ValidateFilters(nameFilter, kindFilter)
            End Sub,
            Function(config As BridgeConfig, map As MapDatabase)
                Dim scope As ResolvedScope = ScopeResolver.Resolve(key, config, map)
                Seam()
                Return SymbolSearchReader.Read(map, scope, config, nameFilter, kindFilter, projectSymbolId)
            End Function)
    End Function

    ''' <summary>
    ''' One symbol whole: parts and edges under the eight verbs (058 §3.3).
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <param name="symbolId">The mapped symbol id.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function SymbolDetail(rawArguments As IReadOnlyDictionary(Of String, JsonElement), Optional symbolId As Long? = Nothing, Optional solutionKey As String = Nothing) As CallToolResult
        Dim key As String = Normalise(solutionKey)
        Return RunRead(Of SymbolDetailEnvelope)(
            Sub()
                ScopeResolver.ValidateArguments(rawArguments, key)
                SymbolResolver.RequireSymbolId(symbolId)
            End Sub,
            Function(config As BridgeConfig, map As MapDatabase)
                Dim scope As ResolvedScope = ScopeResolver.Resolve(key, config, map)
                Seam()
                Return SymbolDetailReader.Read(map, scope, config, symbolId.Value)
            End Function)
    End Function

    ''' <summary>
    ''' Every occurrence targeting a symbol by identity (058 §3.4).
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <param name="symbolId">The mapped symbol id.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function References(rawArguments As IReadOnlyDictionary(Of String, JsonElement), Optional symbolId As Long? = Nothing, Optional solutionKey As String = Nothing) As CallToolResult
        Dim key As String = Normalise(solutionKey)
        Return RunRead(Of ReferencesEnvelope)(
            Sub()
                ScopeResolver.ValidateArguments(rawArguments, key)
                SymbolResolver.RequireSymbolId(symbolId)
            End Sub,
            Function(config As BridgeConfig, map As MapDatabase)
                Dim scope As ResolvedScope = ScopeResolver.Resolve(key, config, map)
                Seam()
                Return ReferencesReader.Read(map, scope, config, symbolId.Value)
            End Function)
    End Function

    ''' <summary>
    ''' Active symbols no recorded reference reaches (060 §3.1).
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <param name="kind">The kind filter, or Nothing.</param>
    ''' <param name="projectSymbolId">A project row to narrow to, or Nothing.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function Orphans(rawArguments As IReadOnlyDictionary(Of String, JsonElement), Optional solutionKey As String = Nothing, Optional kind As String = Nothing, Optional projectSymbolId As Long? = Nothing) As CallToolResult
        Dim key As String = Normalise(solutionKey)
        Dim kindFilter As String = Normalise(kind)
        Return RunRead(Of OrphansEnvelope)(
            Sub()
                ScopeResolver.ValidateArguments(rawArguments, key)
                OrphansReader.ValidateArguments(kindFilter)
            End Sub,
            Function(config As BridgeConfig, map As MapDatabase)
                Dim scope As ResolvedScope = ScopeResolver.Resolve(key, config, map)
                Seam()
                Return OrphansReader.Read(map, scope, config, kindFilter, projectSymbolId)
            End Function)
    End Function

    ''' <summary>
    ''' Everything the map records against one type (contracts/tools.md §3.6).
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <param name="symbolId">The mapped symbol id of a type.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function TypeUsages(rawArguments As IReadOnlyDictionary(Of String, JsonElement), Optional symbolId As Long? = Nothing, Optional solutionKey As String = Nothing) As CallToolResult
        Dim key As String = Normalise(solutionKey)
        Return RunRead(Of TypeUsagesEnvelope)(
            Sub()
                ScopeResolver.ValidateArguments(rawArguments, key)
                SymbolResolver.RequireSymbolId(symbolId)
            End Sub,
            Function(config As BridgeConfig, map As MapDatabase)
                Dim scope As ResolvedScope = ScopeResolver.Resolve(key, config, map)
                Seam()
                Return TypeUsagesReader.Read(map, scope, config, symbolId.Value)
            End Function)
    End Function

    ''' <summary>
    ''' One verdict per map solution, and the directories observed but not in the map (005 contracts/tools.md §3.7).
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function MapStatus(rawArguments As IReadOnlyDictionary(Of String, JsonElement)) As CallToolResult
        Return RunRead(Of MapStatusEnvelope)(
            Sub()
                ScopeResolver.RefuseProjectId(rawArguments)
            End Sub,
            Function(config As BridgeConfig, map As MapDatabase)
                Seam()
                Return MapStatusReader.Read(config, map)
            End Function)
    End Function

    ''' <summary>
    ''' Runs the extractor for one mapped solution, or for every stale one (contracts/tools.md §3.8).
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <param name="solutionKey">A map solution key, or Nothing.</param>
    ''' <param name="repoPath">A directory under a mapped root, or Nothing.</param>
    ''' <param name="stale">True to refresh every stale mapped solution, or Nothing.</param>
    ''' <returns>The result or a refusal.</returns>
    Public Function Extract(rawArguments As IReadOnlyDictionary(Of String, JsonElement), Optional solutionKey As String = Nothing, Optional repoPath As String = Nothing, Optional stale As Boolean? = Nothing) As CallToolResult
        Try
            ScopeResolver.RefuseProjectId(rawArguments)
        Catch ex As BridgeRefusalException
            Return Refuse(ex.Refusal)
        End Try
        Dim request As ExtractRequest = New ExtractRequest With {.Origin = ExtractOrigin.Tool, .SolutionKey = Normalise(solutionKey), .RepoPath = Normalise(repoPath), .Stale = stale.HasValue AndAlso stale.Value}
        If request.Stale Then
            Dim staleResult As StaleResult = _door.RunStale(request)
            Return Wire(BridgeJson.Serialize(staleResult), staleResult.IsFailure)
        End If
        Dim result As ExtractResult = _door.Run(request)
        Return Wire(BridgeJson.Serialize(result), result.IsFailure)
    End Function

    ''' <summary>
    ''' An extract result on the wire: the JSON as the one text block, IsError when refused, failed or timed out (contracts/tools.md §3.8).
    ''' </summary>
    Private Shared Function Wire(json As String, isError As Boolean) As CallToolResult
        Return New CallToolResult With {.IsError = isError, .Content = New List(Of ContentBlock) From {New TextContentBlock With {.Text = json}}}
    End Function

    ''' <summary>
    ''' The one shape every read tool walks: arguments, configuration, map, question; a refusal at any stage is answered as text, and a
    ''' driver failure during the reads is named through the map door.
    ''' </summary>
    Private Function RunRead(Of T)(arguments As Action, body As Func(Of BridgeConfig, MapDatabase, T)) As CallToolResult
        Dim config As BridgeConfig = Nothing
        Try
            arguments()
            config = BridgeConfigFile.Load(_configPath)
            Using map As MapDatabase = MapAccess.OpenRead(config)
                Dim json As String = BridgeJson.Serialize(body(config, map))
                map.EndRead()
                Return Answer(json)
            End Using
        Catch ex As BridgeRefusalException
            Return Refuse(ex.Refusal)
        Catch ex As SqliteException
            Return Refuse(MapAccess.Translate(ex, config).Refusal)
        End Try
    End Function

    Private Sub Seam()
        If _seams IsNot Nothing AndAlso _seams.AfterScopeResolved IsNot Nothing Then _seams.AfterScopeResolved.Invoke()
    End Sub

    Private Shared Function Normalise(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return Nothing
        Return value.Trim()
    End Function

    Private Shared Function Answer(json As String) As CallToolResult
        Return New CallToolResult With {.IsError = False, .Content = New List(Of ContentBlock) From {New TextContentBlock With {.Text = json}}}
    End Function

    Private Shared Function Refuse(refusal As BridgeRefusal) As CallToolResult
        Return New CallToolResult With {.IsError = True, .Content = New List(Of ContentBlock) From {New TextContentBlock With {.Text = refusal.Text}}}
    End Function

End Class

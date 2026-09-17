' File: BridgeHost.vb
' Project: CodeMem.Tests
' Description: In-process host for the fast facts: dispatches a tool name and arguments to BridgeTools; never the production-route evidence (CON3; feature 004, research R54).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' The dispatch method is Invoke, not Call as the tasks named it: Call is a VB keyword.
' 2026-09-15 (T037): Launcher, Door and ConfigPath; WriteConfig takes an optional path so a fact can rewrite its own file.
' 2026-09-17 (feature 005, T013/T014): WriteConfig writes no storePath (FR-403); Invoke hands BridgeTools the raw argument dictionary it
' would receive from the SDK (research R68), so a projectId passed here is refused exactly as over stdio.

Imports System.IO
Imports System.Text.Json
Imports CodeMem.Bridging
Imports ModelContextProtocol.Protocol

''' <summary>
''' Wraps one <see cref="BridgeTools"/> with a configuration file and optional read seams; <see cref="WriteConfig"/> mints the file.
''' </summary>
Public Class BridgeHost

    Private ReadOnly _tools As BridgeTools

    ''' <summary>The configuration file this host reads on every call.</summary>
    Public ReadOnly Property ConfigPath As String

    ''' <summary>The scripted launcher the extract door uses: records every request, returns what it is told (T037).</summary>
    Public ReadOnly Property Launcher As ScriptedLauncher

    ''' <summary>The extract door over the same configuration and launcher, for the origins the tool cannot express.</summary>
    Public ReadOnly Property Door As ExtractDoor

    ''' <summary>
    ''' Binds a host to a configuration file.
    ''' </summary>
    ''' <param name="configPath">The configuration file.</param>
    ''' <param name="seams">Test-only read seams, or Nothing.</param>
    Public Sub New(configPath As String, Optional seams As ReadSeams = Nothing)
        Me.ConfigPath = configPath
        Launcher = New ScriptedLauncher()
        Door = New ExtractDoor(configPath, Launcher)
        _tools = New BridgeTools(configPath, Launcher, seams)
    End Sub

    ''' <summary>
    ''' Calls one tool by name with named arguments (absent keys are Nothing); the whole dictionary also travels as the raw arguments.
    ''' </summary>
    ''' <param name="name">The tool name.</param>
    ''' <param name="args">The arguments.</param>
    ''' <returns>The reply.</returns>
    Public Function Invoke(name As String, args As IDictionary(Of String, Object)) As BridgeReply
        Dim raw As IReadOnlyDictionary(Of String, JsonElement) = RawOf(args)
        Dim result As CallToolResult
        Select Case name
            Case "solutions"
                result = _tools.Solutions(raw)
            Case "symbol_search"
                result = _tools.SymbolSearch(raw, GetString(args, "solutionKey"), GetString(args, "name"), GetString(args, "kind"), GetLong(args, "projectSymbolId"))
            Case "symbol_detail"
                result = _tools.SymbolDetail(raw, GetLong(args, "symbolId"), GetString(args, "solutionKey"))
            Case "references"
                result = _tools.References(raw, GetLong(args, "symbolId"), GetString(args, "solutionKey"))
            Case "orphans"
                result = _tools.Orphans(raw, GetString(args, "solutionKey"), GetString(args, "kind"), GetLong(args, "projectSymbolId"))
            Case "type_usages"
                result = _tools.TypeUsages(raw, GetLong(args, "symbolId"), GetString(args, "solutionKey"))
            Case "map_status"
                result = _tools.MapStatus(raw)
            Case "extract"
                result = _tools.Extract(raw, GetString(args, "solutionKey"), GetString(args, "solutionPath"), GetString(args, "repoPath"), GetBoolean(args, "stale"))
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(name), name, "unknown tool")
        End Select
        Return New BridgeReply(result)
    End Function

    ''' <summary>
    ''' Writes a configuration file at a fresh temp path: the map, the optional extractor and the two gates; no store (feature 005).
    ''' </summary>
    ''' <param name="mapPath">The map.</param>
    ''' <param name="extractorPath">The extractor, or Nothing for the default beside the bridge.</param>
    ''' <param name="enabled">extract.enabled.</param>
    ''' <param name="onGreenBuild">extract.onGreenBuild.</param>
    ''' <param name="path">The file to write, or Nothing for a fresh temp path (a fact rewrites its own file to prove the per-call read).</param>
    ''' <returns>The file's path.</returns>
    Public Shared Function WriteConfig(mapPath As String, extractorPath As String, enabled As Boolean, onGreenBuild As Boolean, Optional path As String = Nothing) As String
        Dim dir As String = IO.Path.Combine(IO.Path.GetTempPath(), "codemem-tests")
        Directory.CreateDirectory(dir)
        Dim file As String = If(path, IO.Path.Combine(dir, "bridge-config-" & Guid.NewGuid().ToString("N") & ".json"))
        Dim document As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal) From {
            {"mapPath", mapPath},
            {"extractorPath", extractorPath},
            {"extract", New Dictionary(Of String, Object)(StringComparer.Ordinal) From {{"enabled", enabled}, {"onGreenBuild", onGreenBuild}}}}
        IO.File.WriteAllText(file, JsonSerializer.Serialize(document))
        Return file
    End Function

    Private Shared Function RawOf(args As IDictionary(Of String, Object)) As IReadOnlyDictionary(Of String, JsonElement)
        If args Is Nothing Then Return Nothing
        Dim raw As Dictionary(Of String, JsonElement) = New Dictionary(Of String, JsonElement)(StringComparer.Ordinal)
        For Each pair As KeyValuePair(Of String, Object) In args
            raw(pair.Key) = JsonSerializer.SerializeToElement(pair.Value)
        Next
        Return raw
    End Function

    Private Shared Function GetLong(args As IDictionary(Of String, Object), key As String) As Long?
        Dim value As Object = Nothing
        If args Is Nothing OrElse Not args.TryGetValue(key, value) OrElse value Is Nothing Then Return Nothing
        Return Convert.ToInt64(value, Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Shared Function GetString(args As IDictionary(Of String, Object), key As String) As String
        Dim value As Object = Nothing
        If args Is Nothing OrElse Not args.TryGetValue(key, value) OrElse value Is Nothing Then Return Nothing
        Return CStr(value)
    End Function

    Private Shared Function GetBoolean(args As IDictionary(Of String, Object), key As String) As Boolean?
        Dim value As Object = Nothing
        If args Is Nothing OrElse Not args.TryGetValue(key, value) OrElse value Is Nothing Then Return Nothing
        Return CBool(value)
    End Function

End Class

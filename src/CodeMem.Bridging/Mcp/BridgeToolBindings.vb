' File: BridgeToolBindings.vb
' Project: CodeMem.Bridging
' Description: The eight methods the MCP server registers: each takes the SDK's request context (injected, absent from the advertised schema) and the typed arguments, and hands BridgeTools the raw argument dictionary (research R68; 005 FR-406).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Why a second class: a RequestContext cannot be built outside a live server (its constructors take the McpServer and the JSON-RPC request),
' so BridgeTools takes a plain dictionary the in-process host can supply, and this class is the one place the SDK type appears. The Optional
' parameters are what the SDK reads for the schema (AddressOf keeps the MethodInfo and its defaults), exactly as 004's BridgeTools did.

Imports System.Text.Json
Imports ModelContextProtocol.Protocol
Imports ModelContextProtocol.Server

''' <summary>
''' One instance per server, over one <see cref="BridgeTools"/>.
''' </summary>
Public Class BridgeToolBindings

    Private ReadOnly _tools As BridgeTools

    ''' <summary>
    ''' Binds to the tools.
    ''' </summary>
    ''' <param name="tools">The tools.</param>
    Public Sub New(tools As BridgeTools)
        _tools = tools
    End Sub

    ''' <summary>solutions.</summary>
    ''' <param name="context">The request context the SDK injects.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function Solutions(context As RequestContext(Of CallToolRequestParams)) As CallToolResult
        Return _tools.Solutions(ArgumentsOf(context))
    End Function

    ''' <summary>symbol_search.</summary>
    ''' <param name="context">The request context the SDK injects.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <param name="name">The name filter, or Nothing.</param>
    ''' <param name="kind">The kind filter, or Nothing.</param>
    ''' <param name="projectSymbolId">A project row to narrow to, or Nothing.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function SymbolSearch(context As RequestContext(Of CallToolRequestParams), Optional solutionKey As String = Nothing, Optional name As String = Nothing, Optional kind As String = Nothing, Optional projectSymbolId As Long? = Nothing) As CallToolResult
        Return _tools.SymbolSearch(ArgumentsOf(context), solutionKey, name, kind, projectSymbolId)
    End Function

    ''' <summary>symbol_detail.</summary>
    ''' <param name="context">The request context the SDK injects.</param>
    ''' <param name="symbolId">The mapped symbol id.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function SymbolDetail(context As RequestContext(Of CallToolRequestParams), Optional symbolId As Long? = Nothing, Optional solutionKey As String = Nothing) As CallToolResult
        Return _tools.SymbolDetail(ArgumentsOf(context), symbolId, solutionKey)
    End Function

    ''' <summary>references.</summary>
    ''' <param name="context">The request context the SDK injects.</param>
    ''' <param name="symbolId">The mapped symbol id.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function References(context As RequestContext(Of CallToolRequestParams), Optional symbolId As Long? = Nothing, Optional solutionKey As String = Nothing) As CallToolResult
        Return _tools.References(ArgumentsOf(context), symbolId, solutionKey)
    End Function

    ''' <summary>orphans.</summary>
    ''' <param name="context">The request context the SDK injects.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <param name="kind">The kind filter, or Nothing.</param>
    ''' <param name="projectSymbolId">A project row to narrow to, or Nothing.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function Orphans(context As RequestContext(Of CallToolRequestParams), Optional solutionKey As String = Nothing, Optional kind As String = Nothing, Optional projectSymbolId As Long? = Nothing) As CallToolResult
        Return _tools.Orphans(ArgumentsOf(context), solutionKey, kind, projectSymbolId)
    End Function

    ''' <summary>type_usages.</summary>
    ''' <param name="context">The request context the SDK injects.</param>
    ''' <param name="symbolId">The mapped symbol id of a type.</param>
    ''' <param name="solutionKey">A map solution key.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function TypeUsages(context As RequestContext(Of CallToolRequestParams), Optional symbolId As Long? = Nothing, Optional solutionKey As String = Nothing) As CallToolResult
        Return _tools.TypeUsages(ArgumentsOf(context), symbolId, solutionKey)
    End Function

    ''' <summary>map_status.</summary>
    ''' <param name="context">The request context the SDK injects.</param>
    ''' <returns>The envelope or a refusal.</returns>
    Public Function MapStatus(context As RequestContext(Of CallToolRequestParams)) As CallToolResult
        Return _tools.MapStatus(ArgumentsOf(context))
    End Function

    ''' <summary>extract.</summary>
    ''' <param name="context">The request context the SDK injects.</param>
    ''' <param name="solutionKey">A map solution key, or Nothing.</param>
    ''' <param name="repoPath">A directory under a mapped root, or Nothing.</param>
    ''' <param name="stale">True to refresh every stale mapped solution, or Nothing.</param>
    ''' <returns>The result or a refusal.</returns>
    Public Function Extract(context As RequestContext(Of CallToolRequestParams), Optional solutionKey As String = Nothing, Optional repoPath As String = Nothing, Optional stale As Boolean? = Nothing) As CallToolResult
        Return _tools.Extract(ArgumentsOf(context), solutionKey, repoPath, stale)
    End Function

    ''' <summary>
    ''' The call's arguments as received, or Nothing when the request carries none.
    ''' </summary>
    ''' <param name="context">The request context.</param>
    ''' <returns>The dictionary.</returns>
    Private Shared Function ArgumentsOf(context As RequestContext(Of CallToolRequestParams)) As IReadOnlyDictionary(Of String, JsonElement)
        If context Is Nothing OrElse context.Params Is Nothing Then Return Nothing
        If context.Params.Arguments Is Nothing Then Return Nothing
        Return New Dictionary(Of String, JsonElement)(context.Params.Arguments, StringComparer.Ordinal)
    End Function

End Class

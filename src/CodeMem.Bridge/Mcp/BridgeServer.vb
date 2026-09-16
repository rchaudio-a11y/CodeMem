' File: BridgeServer.vb
' Project: CodeMem.Bridge
' Description: The MCP host wiring: a stdio server named codemem whose tools are BridgeTools' registered delegates (research R43; CON1: wiring only).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Threading
Imports System.Threading.Tasks
Imports CodeMem.Bridging
Imports ModelContextProtocol.Protocol
Imports ModelContextProtocol.Server

''' <summary>
''' Creates the server over standard input and output, registers one tool per registered name with its description and the read-only
''' annotation (extract is the one tool without it), runs until the client closes the pipe, then disposes (never inside a Finally: VB
''' forbids Await there). Nothing else writes to stdout while it runs.
''' </summary>
Public Module BridgeServer

    ''' <summary>
    ''' Runs the server to completion.
    ''' </summary>
    ''' <param name="configPath">The configuration file every call reads, or Nothing for the file beside the executable.</param>
    ''' <returns>0 when the client closed the pipe.</returns>
    Public Async Function RunAsync(configPath As String) As Task(Of Integer)
        Dim tools As BridgeTools = New BridgeTools(configPath, New ProcessExtractorLauncher(), Nothing)
        Dim options As McpServerOptions = New McpServerOptions()
        options.ServerInfo = New Implementation With {.Name = "codemem", .Version = GetType(BridgeServer).Assembly.GetName().Version.ToString(3)}
        options.ToolCollection = New McpServerPrimitiveCollection(Of McpServerTool)()
        For Each name As String In BridgeTools.RegisteredToolNames
            Dim createOptions As McpServerToolCreateOptions = New McpServerToolCreateOptions With {
                .Name = name,
                .Description = BridgeTools.RegisteredToolDescriptions(name),
                .ReadOnly = (name <> "extract")}
            If name = "extract" Then createOptions.Destructive = False
            options.ToolCollection.Add(McpServerTool.Create(ToolDelegate(tools, name), createOptions))
        Next
        Dim transport As StdioServerTransport = New StdioServerTransport("codemem", Nothing)
        Dim server As McpServer = McpServer.Create(transport, options, Nothing, Nothing)
        Await server.RunAsync(CancellationToken.None)
        Await server.DisposeAsync()
        Return 0
    End Function

    ''' <summary>
    ''' The delegate the SDK builds each tool from: its parameter names, optionality and return type are the tool method's own.
    ''' </summary>
    ''' <param name="tools">The bound tools.</param>
    ''' <param name="name">The registered name.</param>
    ''' <returns>The delegate.</returns>
    Private Function ToolDelegate(tools As BridgeTools, name As String) As [Delegate]
        Select Case name
            Case "solutions"
                Return New Func(Of CallToolResult)(AddressOf tools.Solutions)
            Case "symbol_search"
                Return New Func(Of Long?, String, String, String, Long?, CallToolResult)(AddressOf tools.SymbolSearch)
            Case "symbol_detail"
                Return New Func(Of Long?, Long?, String, CallToolResult)(AddressOf tools.SymbolDetail)
            Case "references"
                Return New Func(Of Long?, Long?, String, CallToolResult)(AddressOf tools.References)
            Case "orphans"
                Return New Func(Of Long?, String, String, Long?, CallToolResult)(AddressOf tools.Orphans)
            Case "type_usages"
                Return New Func(Of Long?, Long?, String, CallToolResult)(AddressOf tools.TypeUsages)
            Case "map_status"
                Return New Func(Of CallToolResult)(AddressOf tools.MapStatus)
            Case "extract"
                Return New Func(Of String, String, Boolean?, CallToolResult)(AddressOf tools.Extract)
            Case Else
                Throw New InvalidOperationException("no delegate for tool " & name)
        End Select
    End Function

End Module

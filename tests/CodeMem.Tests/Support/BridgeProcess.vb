' File: BridgeProcess.vb
' Project: CodeMem.Tests
' Description: Spawns the real CodeMem.Bridge executable from the test output directory and speaks JSON-RPC to it over stdio (feature 004, Article XIII, research R54).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>
''' The production route: <c>dotnet CodeMem.Bridge.dll serve --config &lt;path&gt;</c> driven with the four messages the spike probe sent
''' (initialize, notifications/initialized, tools/list, tools/call), and <see cref="RunOnce"/> for the extract and hook entries with stdin.
''' No bridge type is referenced here, so this class compiles before the bridge has any.
''' </summary>
Public Class BridgeProcess
    Implements IDisposable

    Private Shared ReadOnly ReadTimeout As TimeSpan = TimeSpan.FromSeconds(600)

    Private ReadOnly _process As Process
    Private ReadOnly _stderr As Task(Of String)
    Private _nextId As Integer = 1

    Private Sub New(process As Process)
        _process = process
        _stderr = process.StandardError.ReadToEndAsync()
    End Sub

    ''' <summary>
    ''' Starts <c>serve --config &lt;configPath&gt;</c>.
    ''' </summary>
    ''' <param name="configPath">The configuration file the server reads on every call.</param>
    ''' <returns>The running server; call <see cref="Initialize"/> next and <see cref="Close"/> at the end.</returns>
    Public Shared Function Serve(configPath As String) As BridgeProcess
        Dim psi As ProcessStartInfo = StartInfo("serve --config " & Quote(configPath))
        Return New BridgeProcess(Process.Start(psi))
    End Function

    ''' <summary>
    ''' Sends initialize (protocol 2025-06-18) and notifications/initialized.
    ''' </summary>
    ''' <returns>The serverInfo element of the initialize result.</returns>
    Public Function Initialize() As JsonElement
        Dim result As JsonElement = Request("initialize", "{""protocolVersion"":""2025-06-18"",""capabilities"":{},""clientInfo"":{""name"":""CodeMem.Tests"",""version"":""0""}}")
        Notify("notifications/initialized")
        Return result.GetProperty("serverInfo").Clone()
    End Function

    ''' <summary>
    ''' The tool names tools/list returns, in the server's order.
    ''' </summary>
    ''' <returns>The names.</returns>
    Public Function ListTools() As List(Of String)
        Dim names As List(Of String) = New List(Of String)()
        For Each tool As JsonElement In Request("tools/list", "{}").GetProperty("tools").EnumerateArray()
            names.Add(tool.GetProperty("name").GetString())
        Next
        Return names
    End Function

    ''' <summary>
    ''' The descriptions tools/list returns, by tool name (FR-312 on the production route, analyze pass 2 G4).
    ''' </summary>
    ''' <returns>Name to description.</returns>
    Public Function ListToolDescriptions() As Dictionary(Of String, String)
        Dim descriptions As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
        For Each tool As JsonElement In Request("tools/list", "{}").GetProperty("tools").EnumerateArray()
            Dim description As JsonElement
            descriptions(tool.GetProperty("name").GetString()) = If(tool.TryGetProperty("description", description), description.GetString(), Nothing)
        Next
        Return descriptions
    End Function

    ''' <summary>
    ''' Calls one tool.
    ''' </summary>
    ''' <param name="name">The tool name.</param>
    ''' <param name="argumentsJson">The arguments as a JSON object text.</param>
    ''' <returns>The reply: the isError flag and the first text block.</returns>
    Public Function CallTool(name As String, argumentsJson As String) As ToolReply
        Dim result As JsonElement = Request("tools/call", "{""name"":""" & name & """,""arguments"":" & argumentsJson & "}")
        Dim isErrorElement As JsonElement
        Dim isError As Boolean = result.TryGetProperty("isError", isErrorElement) AndAlso isErrorElement.ValueKind = JsonValueKind.True
        Dim text As String = Nothing
        Dim content As JsonElement
        If result.TryGetProperty("content", content) AndAlso content.GetArrayLength() > 0 Then
            text = content(0).GetProperty("text").GetString()
        End If
        Return New ToolReply(isError, text)
    End Function

    ''' <summary>
    ''' Closes stdin so the server's RunAsync completes, waits for the exit, and returns the exit code (killing after 15 s).
    ''' </summary>
    ''' <returns>The exit code.</returns>
    Public Function Close() As Integer
        Try
            _process.StandardInput.Close()
        Catch ex As IOException
            ' the pipe may already be gone
        End Try
        If Not _process.WaitForExit(15000) Then
            _process.Kill(True)
            _process.WaitForExit()
        End If
        Return _process.ExitCode
    End Function

    ''' <summary>
    ''' Everything the server wrote to standard error (complete after <see cref="Close"/>).
    ''' </summary>
    ''' <returns>The captured text.</returns>
    Public Function StandardError() As String
        Return _stderr.Result
    End Function

    ''' <summary>
    ''' Runs one entry to completion with text on stdin: <c>extract …</c> or <c>hook …</c>.
    ''' </summary>
    ''' <param name="args">The arguments after the dll.</param>
    ''' <param name="stdin">Text written to standard input before it is closed; Nothing for none.</param>
    ''' <returns>The captured result.</returns>
    Public Shared Function RunOnce(args As String, stdin As String) As ProcessReply
        Dim psi As ProcessStartInfo = StartInfo(args)
        Dim watch As Stopwatch = Stopwatch.StartNew()
        Using process As Process = Process.Start(psi)
            Dim stdoutTask As Task(Of String) = process.StandardOutput.ReadToEndAsync()
            Dim stderrTask As Task(Of String) = process.StandardError.ReadToEndAsync()
            If stdin IsNot Nothing Then process.StandardInput.Write(stdin)
            process.StandardInput.Close()
            If Not process.WaitForExit(CInt(ReadTimeout.TotalMilliseconds)) Then
                process.Kill(True)
                process.WaitForExit()
                Throw New TimeoutException("the bridge did not exit within " & ReadTimeout.ToString() & Environment.NewLine & stderrTask.Result)
            End If
            watch.Stop()
            Return New ProcessReply(process.ExitCode, stdoutTask.Result, stderrTask.Result, watch.Elapsed)
        End Using
    End Function

    ''' <summary>
    ''' Wraps a path argument in double quotes.
    ''' </summary>
    ''' <param name="path">The path.</param>
    ''' <returns>The quoted path.</returns>
    Public Shared Function Quote(path As String) As String
        Return """" & path & """"
    End Function

    ''' <summary>
    ''' Kills the server if it is still running.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            If Not _process.HasExited Then
                _process.Kill(True)
                _process.WaitForExit()
            End If
        Catch ex As InvalidOperationException
            ' already gone
        End Try
        _process.Dispose()
    End Sub

    Private Shared Function StartInfo(args As String) As ProcessStartInfo
        Dim dll As String = Path.Combine(AppContext.BaseDirectory, "CodeMem.Bridge.dll")
        If Not File.Exists(dll) Then Throw New FileNotFoundException("bridge not found beside the tests", dll)
        Return New ProcessStartInfo("dotnet", """" & dll & """ " & args) With {
            .WorkingDirectory = AppContext.BaseDirectory,
            .RedirectStandardInput = True,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .StandardInputEncoding = New UTF8Encoding(False),
            .StandardOutputEncoding = New UTF8Encoding(False),
            .StandardErrorEncoding = New UTF8Encoding(False),
            .UseShellExecute = False,
            .CreateNoWindow = True}
    End Function

    Private Function Request(method As String, paramsJson As String) As JsonElement
        Dim id As Integer = _nextId
        _nextId += 1
        Send("{""jsonrpc"":""2.0"",""id"":" & id & ",""method"":""" & method & """,""params"":" & paramsJson & "}")
        Do
            Dim line As String = ReadLine()
            If line Is Nothing Then Throw New InvalidOperationException("the server closed its output before answering " & method & Environment.NewLine & _stderr.Result)
            If line.Trim().Length = 0 Then Continue Do
            Using document As JsonDocument = JsonDocument.Parse(line)
                Dim idElement As JsonElement
                If Not document.RootElement.TryGetProperty("id", idElement) OrElse idElement.ValueKind <> JsonValueKind.Number OrElse idElement.GetInt32() <> id Then Continue Do
                Dim errorElement As JsonElement
                If document.RootElement.TryGetProperty("error", errorElement) Then Throw New InvalidOperationException(method & " failed: " & errorElement.GetRawText())
                Return document.RootElement.GetProperty("result").Clone()
            End Using
        Loop
    End Function

    Private Sub Notify(method As String)
        Send("{""jsonrpc"":""2.0"",""method"":""" & method & """}")
    End Sub

    Private Sub Send(line As String)
        _process.StandardInput.Write(line & vbLf)
        _process.StandardInput.Flush()
    End Sub

    Private Function ReadLine() As String
        Dim pending As Task(Of String) = _process.StandardOutput.ReadLineAsync()
        If Not pending.Wait(ReadTimeout) Then
            _process.Kill(True)
            Throw New TimeoutException("no answer from the bridge within " & ReadTimeout.ToString() & Environment.NewLine & _stderr.Result)
        End If
        Return pending.Result
    End Function

End Class

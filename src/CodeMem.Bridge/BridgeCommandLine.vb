' File: BridgeCommandLine.vb
' Project: CodeMem.Bridge
' Description: Parses the bridge's command line (contracts/cli-config-hook.md §1): the three entries, --config on every entry, the extract options.
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T027): --solution <path> on the extract entry, only beside --solution-key (contracts/cli-config-hook.md §1); there is no
' --key - --solution-key is the one spelling.

''' <summary>
''' The parse result: the entry, its options, a help request, or a usage error. Argument names are case-insensitive; values follow as the
''' next argument (the extractor's CommandLine shape). Grammar only: whether an extract named exactly one target is the extract door's
''' rule (Article XII, INC2), not restated here.
''' </summary>
Public Class BridgeCommandLine

    ''' <summary>The stdio MCP server entry (the default).</summary>
    Public Const ServeEntry As String = "serve"

    ''' <summary>The one-shot extract entry.</summary>
    Public Const ExtractEntry As String = "extract"

    ''' <summary>Claude Code's PostToolUse hook entry.</summary>
    Public Const HookEntry As String = "hook"

    ''' <summary>The entry named: serve, extract or hook.</summary>
    Public Property Entry As String = ServeEntry

    ''' <summary>The configuration file named by --config, or Nothing for the file beside the executable.</summary>
    Public Property ConfigPath As String

    ''' <summary>The --solution-key value, or Nothing.</summary>
    Public Property SolutionKey As String

    ''' <summary>The --repo-path value, or Nothing.</summary>
    Public Property RepoPath As String

    ''' <summary>--solution: the solution file to extract beside --solution-key, or Nothing (feature 005).</summary>
    Public Property SolutionPath As String

    ''' <summary>True when --stale was given.</summary>
    Public Property Stale As Boolean

    ''' <summary>True when --on-green-build was given (the hook entry's origin; a human does not pass it).</summary>
    Public Property OnGreenBuild As Boolean

    ''' <summary>True when --help was given.</summary>
    Public Property ShowHelp As Boolean

    ''' <summary>The usage error, or Nothing.</summary>
    Public Property ErrorText As String

    ''' <summary>
    ''' The usage text: the three entries and their exit codes.
    ''' </summary>
    ''' <returns>The usage text.</returns>
    Public Shared ReadOnly Property Usage As String
        Get
            Return "usage: CodeMem.Bridge [serve] [--config <path>]" & Environment.NewLine &
                "       CodeMem.Bridge extract (--solution-key <key> [--solution <path>] | --repo-path <dir> | --stale) [--on-green-build] [--config <path>]" & Environment.NewLine &
                "       CodeMem.Bridge hook [--config <path>]" & Environment.NewLine &
                "       CodeMem.Bridge --help" & Environment.NewLine &
                Environment.NewLine &
                "entries: serve    the stdio MCP server (default); stdout carries the transport and nothing else; exit 0 when the client closes the pipe, 1 on a startup failure" & Environment.NewLine &
                "         extract  one extraction through the door; prints the result JSON; exit 0 when the request was accepted and the child ran, 2 on a refusal, 1 on a usage error" & Environment.NewLine &
                "         hook     Claude Code's PostToolUse hook: reads the event JSON from stdin, prints one JSON line, always exits 0" & Environment.NewLine &
                Environment.NewLine &
                "--config names the configuration file; without it, bridge.config.json beside the executable." & Environment.NewLine &
                "--on-green-build marks the green-build origin; the hook entry uses it, a human does not." & Environment.NewLine &
                "--solution names the solution file (.sln or .slnx) to extract beside --solution-key: the way a solution the map has never seen is added." & Environment.NewLine &
                "There is no --key; --solution-key is the one spelling." & Environment.NewLine
        End Get
    End Property

    ''' <summary>
    ''' Parses the arguments.
    ''' </summary>
    ''' <param name="args">The raw arguments.</param>
    ''' <returns>The result.</returns>
    Public Shared Function Parse(args As String()) As BridgeCommandLine
        Dim result As BridgeCommandLine = New BridgeCommandLine()
        Dim i As Integer = 0
        If args.Length > 0 AndAlso Not args(0).StartsWith("-", StringComparison.Ordinal) AndAlso Not args(0).StartsWith("/", StringComparison.Ordinal) Then
            Dim first As String = args(0).ToLowerInvariant()
            If first = ServeEntry OrElse first = ExtractEntry OrElse first = HookEntry Then
                result.Entry = first
                i = 1
            Else
                result.ErrorText = "unknown entry: " & args(0)
                Return result
            End If
        End If
        While i < args.Length
            Dim name As String = args(i).ToLowerInvariant()
            If name = "--help" OrElse name = "-h" OrElse name = "/?" Then
                result.ShowHelp = True
                Return result
            End If
            Select Case name
                Case "--config"
                    If i + 1 >= args.Length Then Return MissingValue(result, args(i))
                    result.ConfigPath = args(i + 1)
                    i += 2
                Case "--solution-key"
                    If i + 1 >= args.Length Then Return MissingValue(result, args(i))
                    If result.Entry <> ExtractEntry Then Return ExtractOnly(result, args(i))
                    result.SolutionKey = args(i + 1)
                    i += 2
                Case "--solution"
                    If i + 1 >= args.Length Then Return MissingValue(result, args(i))
                    If result.Entry <> ExtractEntry Then Return ExtractOnly(result, args(i))
                    result.SolutionPath = args(i + 1)
                    i += 2
                Case "--repo-path"
                    If i + 1 >= args.Length Then Return MissingValue(result, args(i))
                    If result.Entry <> ExtractEntry Then Return ExtractOnly(result, args(i))
                    result.RepoPath = args(i + 1)
                    i += 2
                Case "--stale"
                    If result.Entry <> ExtractEntry Then Return ExtractOnly(result, args(i))
                    result.Stale = True
                    i += 1
                Case "--on-green-build"
                    If result.Entry <> ExtractEntry Then Return ExtractOnly(result, args(i))
                    result.OnGreenBuild = True
                    i += 1
                Case Else
                    result.ErrorText = "unknown argument: " & args(i)
                    Return result
            End Select
        End While
        If result.SolutionPath IsNot Nothing AndAlso result.SolutionKey Is Nothing Then result.ErrorText = "--solution applies only beside --solution-key"
        Return result
    End Function

    Private Shared Function MissingValue(result As BridgeCommandLine, name As String) As BridgeCommandLine
        result.ErrorText = "missing value for " & name
        Return result
    End Function

    Private Shared Function ExtractOnly(result As BridgeCommandLine, name As String) As BridgeCommandLine
        result.ErrorText = name & " applies to the extract entry only"
        Return result
    End Function

End Class

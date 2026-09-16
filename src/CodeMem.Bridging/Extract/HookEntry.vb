' File: HookEntry.vb
' Project: CodeMem.Bridging
' Description: The hook entry: reads the PostToolUse JSON from stdin, hands the door the built directory with the green-build origin, answers Claude Code with one JSON line, always exits 0 (contracts/cli-config-hook.md §1 steps 5-6; FR-335, FR-338; research R42).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO
Imports System.Text.Json

''' <summary>
''' Never fails the tool call it observed: every outcome, a refusal, a child failure or an exception, is one line of additionalContext.
''' </summary>
Public Module HookEntry

    ''' <summary>
    ''' Runs the entry.
    ''' </summary>
    ''' <param name="configPath">The configuration file, or Nothing for the file beside the executable.</param>
    ''' <param name="input">Standard input.</param>
    ''' <param name="output">Standard output; exactly one line is written.</param>
    ''' <returns>Always 0.</returns>
    Public Function Run(configPath As String, input As TextReader, output As TextWriter) As Integer
        Dim line As String
        Try
            Dim request As HookRequest = HookRequest.Parse(input.ReadToEnd())
            If request.Answer IsNot Nothing Then
                line = request.Answer
            Else
                Dim door As ExtractDoor = New ExtractDoor(configPath, New ProcessExtractorLauncher())
                Dim result As ExtractResult = door.Run(New ExtractRequest With {.Origin = ExtractOrigin.GreenBuild, .RepoPath = request.Target})
                Dim prefix As String = "codemem extract (green build, " & request.Target & "): "
                If result.Refusal IsNot Nothing AndAlso Not result.Launched Then
                    line = prefix & If(result.Target.ResolvedKey Is Nothing, "", result.Target.ResolvedKey & ": ") & "refused — " & result.Refusal.Text
                ElseIf result.TimedOut Then
                    line = prefix & result.Target.ResolvedKey & ": timed out — " & result.Line
                Else
                    line = prefix & result.Target.ResolvedKey & ": exit " & If(result.ExitCode.HasValue, result.ExitCode.Value.ToString(Globalization.CultureInfo.InvariantCulture), "-") & " — " & If(result.Line, "")
                End If
            End If
        Catch ex As Exception
            line = "codemem hook: failed — " & ex.Message
        End Try
        Dim payload As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal) From {
            {"hookSpecificOutput", New Dictionary(Of String, Object)(StringComparer.Ordinal) From {
                {"hookEventName", "PostToolUse"},
                {"additionalContext", OneLine(line)}}}}
        output.WriteLine(JsonSerializer.Serialize(payload))
        output.Flush()
        Return 0
    End Function

    Private Function OneLine(text As String) As String
        Return If(text, "").Replace(vbCrLf, " ").Replace(vbLf, " ").Replace(vbCr, " ")
    End Function

End Module

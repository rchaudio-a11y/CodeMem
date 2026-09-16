' File: HookRequest.vb
' Project: CodeMem.Bridging
' Description: Parses a PostToolUse payload into the repoPath the hook hands the door, or a named non-case (contracts/cli-config-hook.md §1 steps 1-4; FR-335, spec Q6, COR2; research R42, R48).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions

''' <summary>
''' The command is split on chain separators; the first dotnet build or test segment is the target; an earlier cd sets the effective
''' directory; positional targets are parsed after options, by shape, never checked for existence; more than one candidate is ambiguous,
''' never a fallback.
''' </summary>
Public Class HookRequest

    ''' <summary>The one-line answer for a non-case, or Nothing when the door is to be called.</summary>
    Public Property Answer As String

    ''' <summary>The directory handed to the door as repoPath, or Nothing.</summary>
    Public Property Target As String

    ''' <summary>
    ''' Parses the payload.
    ''' </summary>
    ''' <param name="json">The event JSON from stdin.</param>
    ''' <returns>The request.</returns>
    Public Shared Function Parse(json As String) As HookRequest
        Dim request As HookRequest = New HookRequest()
        Dim root As JsonElement
        Try
            Using document As JsonDocument = JsonDocument.Parse(json)
                root = document.RootElement.Clone()
            End Using
        Catch ex As JsonException
            request.Answer = "codemem hook: payload not JSON; nothing ran."
            Return request
        End Try
        If root.ValueKind <> JsonValueKind.Object Then
            request.Answer = "codemem hook: payload not JSON; nothing ran."
            Return request
        End If
        Dim eventName As String = Text(root, "hook_event_name")
        Dim toolName As String = Text(root, "tool_name")
        If eventName <> "PostToolUse" OrElse toolName <> "Bash" Then
            request.Answer = "codemem hook: " & If(eventName, "?") & "/" & If(toolName, "?") & " is not a Bash PostToolUse; nothing ran."
            Return request
        End If
        Dim toolResponse As JsonElement
        If root.TryGetProperty("tool_response", toolResponse) AndAlso toolResponse.ValueKind = JsonValueKind.Object Then
            Dim interrupted As JsonElement
            If toolResponse.TryGetProperty("interrupted", interrupted) AndAlso interrupted.ValueKind = JsonValueKind.True Then
                request.Answer = "codemem hook: the tool call was interrupted; nothing ran."
                Return request
            End If
        End If
        Dim command As String = Nothing
        Dim toolInput As JsonElement
        If root.TryGetProperty("tool_input", toolInput) AndAlso toolInput.ValueKind = JsonValueKind.Object Then command = Text(toolInput, "command")
        Dim cwd As String = Text(root, "cwd")
        Dim segments As String() = Regex.Split(If(command, ""), "&&|;|\|")
        Dim buildIndex As Integer = -1
        For i As Integer = 0 To segments.Length - 1
            Dim trimmed As String = segments(i).Trim()
            If trimmed.StartsWith("dotnet build", StringComparison.OrdinalIgnoreCase) OrElse trimmed.StartsWith("dotnet test", StringComparison.OrdinalIgnoreCase) Then
                buildIndex = i
                Exit For
            End If
        Next
        If buildIndex < 0 Then
            request.Answer = "codemem hook: not a dotnet build or test; nothing ran."
            Return request
        End If
        Dim effective As String = If(String.IsNullOrWhiteSpace(cwd), Environment.CurrentDirectory, cwd)
        For i As Integer = 0 To buildIndex - 1
            Dim trimmed As String = segments(i).Trim()
            If trimmed.StartsWith("cd ", StringComparison.OrdinalIgnoreCase) Then
                effective = Path.GetFullPath(Path.Combine(effective, Unquote(trimmed.Substring(3).Trim())))
            End If
        Next
        Dim tokens As List(Of String) = Tokenise(segments(buildIndex).Trim())
        Dim candidates As List(Of String) = New List(Of String)()
        For i As Integer = 2 To tokens.Count - 1
            If IsSolutionOrProject(tokens(i)) Then candidates.Add(tokens(i))
        Next
        If tokens.Count > 2 AndAlso Not tokens(2).StartsWith("-", StringComparison.Ordinal) AndAlso Not IsSolutionOrProject(tokens(2)) Then candidates.Add(tokens(2))
        If candidates.Count = 0 Then
            request.Target = effective
        ElseIf candidates.Count = 1 Then
            Dim resolved As String = Path.GetFullPath(Path.Combine(effective, candidates(0)))
            request.Target = If(IsSolutionOrProject(candidates(0)), Path.GetDirectoryName(resolved), resolved)
        Else
            Dim ambiguous As BridgeRefusal = BridgeRefusal.Named(BridgeRefusalKind.AmbiguousTarget, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"candidates", String.Join(", ", candidates)}})
            request.Answer = "codemem hook: refused — " & ambiguous.Text
        End If
        Return request
    End Function

    Private Shared Function IsSolutionOrProject(token As String) As Boolean
        Return token.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) OrElse token.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Function Text(element As JsonElement, name As String) As String
        Dim value As JsonElement
        If element.TryGetProperty(name, value) AndAlso value.ValueKind = JsonValueKind.String Then Return value.GetString()
        Return Nothing
    End Function

    Private Shared Function Unquote(value As String) As String
        If value.Length >= 2 AndAlso ((value.StartsWith("""", StringComparison.Ordinal) AndAlso value.EndsWith("""", StringComparison.Ordinal)) OrElse (value.StartsWith("'", StringComparison.Ordinal) AndAlso value.EndsWith("'", StringComparison.Ordinal))) Then
            Return value.Substring(1, value.Length - 2)
        End If
        Return value
    End Function

    ''' <summary>
    ''' Splits a command segment on whitespace, keeping quoted tokens whole and stripping their quotes.
    ''' </summary>
    ''' <param name="segment">The build segment.</param>
    ''' <returns>The tokens.</returns>
    Public Shared Function Tokenise(segment As String) As List(Of String)
        Dim tokens As List(Of String) = New List(Of String)()
        Dim current As Text.StringBuilder = New Text.StringBuilder()
        Dim quote As Char = Nothing
        Dim inToken As Boolean = False
        For Each c As Char In segment
            If quote <> Nothing Then
                If c = quote Then
                    quote = Nothing
                Else
                    current.Append(c)
                End If
            ElseIf c = """"c OrElse c = "'"c Then
                quote = c
                inToken = True
            ElseIf Char.IsWhiteSpace(c) Then
                If inToken Then
                    tokens.Add(current.ToString())
                    current.Clear()
                    inToken = False
                End If
            Else
                current.Append(c)
                inToken = True
            End If
        Next
        If inToken Then tokens.Add(current.ToString())
        Return tokens
    End Function

End Class

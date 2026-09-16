' File: ToolReply.vb
' Project: CodeMem.Tests
' Description: One tools/call reply from the spawned bridge: the error flag and the text block, parsed as JSON when it is JSON (feature 004).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Text.Json

''' <summary>
''' The result of <c>BridgeProcess.CallTool</c>: <see cref="IsError"/> from the wire, the first text content block, and its parse.
''' </summary>
Public Class ToolReply

    ''' <summary>True when the server answered with isError.</summary>
    Public ReadOnly Property IsError As Boolean

    ''' <summary>The text of the first content block (an envelope or a refusal sentence).</summary>
    Public ReadOnly Property Text As String

    ''' <summary>The text parsed as JSON, or Nothing when it is not a JSON document.</summary>
    Public ReadOnly Property Json As JsonDocument

    ''' <summary>
    ''' Wraps one reply.
    ''' </summary>
    ''' <param name="isError">The isError flag.</param>
    ''' <param name="text">The first content block's text.</param>
    Public Sub New(isError As Boolean, text As String)
        Me.IsError = isError
        Me.Text = text
        If text IsNot Nothing AndAlso text.TrimStart().StartsWith("{", StringComparison.Ordinal) Then
            Try
                Json = JsonDocument.Parse(text)
            Catch ex As JsonException
                Json = Nothing
            End Try
        End If
    End Sub

    ''' <summary>
    ''' The root element of the parsed text.
    ''' </summary>
    ''' <returns>The root; throws when the text is not JSON.</returns>
    Public Function Root() As JsonElement
        If Json Is Nothing Then Throw New InvalidOperationException("the reply is not JSON: " & Text)
        Return Json.RootElement
    End Function

End Class

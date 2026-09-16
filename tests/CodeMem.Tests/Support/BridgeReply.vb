' File: BridgeReply.vb
' Project: CodeMem.Tests
' Description: One in-process tool reply: the error flag, the first text block and its JSON parse (feature 004).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Text.Json
Imports ModelContextProtocol.Protocol

''' <summary>
''' The result of <c>BridgeHost.Invoke</c>: <see cref="IsError"/>, the text of the first content block, and the parsed document when it is JSON.
''' </summary>
Public Class BridgeReply

    ''' <summary>True when the tool refused.</summary>
    Public ReadOnly Property IsError As Boolean

    ''' <summary>The first content block's text.</summary>
    Public ReadOnly Property Text As String

    ''' <summary>The text parsed as JSON, or Nothing.</summary>
    Public ReadOnly Property Json As JsonDocument

    ''' <summary>
    ''' Unwraps a CallToolResult.
    ''' </summary>
    ''' <param name="result">The tool's result.</param>
    Public Sub New(result As CallToolResult)
        IsError = result.IsError.HasValue AndAlso result.IsError.Value
        If result.Content IsNot Nothing AndAlso result.Content.Count > 0 Then
            Dim block As TextContentBlock = TryCast(result.Content(0), TextContentBlock)
            If block IsNot Nothing Then Text = block.Text
        End If
        If Text IsNot Nothing AndAlso Text.TrimStart().StartsWith("{", StringComparison.Ordinal) Then
            Try
                Json = JsonDocument.Parse(Text)
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

' File: OccurrenceAssertions.vb
' Project: CodeMem.Tests
' Description: The identity invariant every returned occurrence must satisfy (FR-348, SC-307): a mapped symbol id, or external with a doc-comment id, never neither (feature 004).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Text.Json
Imports Xunit

''' <summary>
''' Applied by B02, B03 and B07 to every occurrence a tool returns, in each of the wire shapes: a nested target (type_usages), the flat
''' targetSymbolId / external / targetDocCommentId of symbol_detail's outbound groups, the flat sourceSymbolId of its inbound groups, and
''' the source of references (whose target is the asked, mapped symbol itself).
''' </summary>
Public Module OccurrenceAssertions

    ''' <summary>
    ''' Asserts the invariant on one occurrence element.
    ''' </summary>
    ''' <param name="occurrence">The occurrence.</param>
    Public Sub AssertIdentity(occurrence As JsonElement)
        Dim target As JsonElement
        If occurrence.TryGetProperty("target", target) Then
            AssertTarget(target.GetProperty("id"), target.GetProperty("external"), target.GetProperty("docCommentId"))
        End If
        Dim flatTargetId As JsonElement
        If occurrence.TryGetProperty("targetSymbolId", flatTargetId) Then
            AssertTarget(flatTargetId, occurrence.GetProperty("external"), occurrence.GetProperty("targetDocCommentId"))
        End If
        Dim source As JsonElement
        If occurrence.TryGetProperty("source", source) Then
            Assert.Equal(JsonValueKind.Number, source.GetProperty("id").ValueKind)
        End If
        Dim flatSourceId As JsonElement
        If occurrence.TryGetProperty("sourceSymbolId", flatSourceId) Then
            Assert.Equal(JsonValueKind.Number, flatSourceId.ValueKind)
        End If
    End Sub

    Private Sub AssertTarget(id As JsonElement, external As JsonElement, docCommentId As JsonElement)
        If id.ValueKind = JsonValueKind.Null Then
            Assert.True(external.ValueKind = JsonValueKind.True, "a target with no id is not marked external")
            Assert.True(docCommentId.ValueKind = JsonValueKind.String AndAlso docCommentId.GetString().Length > 0, "an external target carries no doc-comment id")
        Else
            Assert.Equal(JsonValueKind.Number, id.ValueKind)
            Assert.True(external.ValueKind = JsonValueKind.False, "a mapped target is marked external")
        End If
    End Sub

End Module

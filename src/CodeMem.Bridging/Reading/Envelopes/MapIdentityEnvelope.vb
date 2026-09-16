' File: MapIdentityEnvelope.vb
' Project: CodeMem.Bridging
' Description: The map identity of the solutions result (056 §1.1 map).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Text.Json.Serialization

''' <summary>
''' guid, schemaVersion, createdUtc.
''' </summary>
Public Class MapIdentityEnvelope

    ''' <summary>The map GUID.</summary>
    <JsonPropertyName("guid")>
    Public Property MapGuid As String

    ''' <summary>The stored schema version.</summary>
    Public Property SchemaVersion As Integer

    ''' <summary>Creation time.</summary>
    Public Property CreatedUtc As String

End Class

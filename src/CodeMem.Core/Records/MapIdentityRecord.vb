' File: MapIdentityRecord.vb
' Project: CodeMem.Core
' Description: The one map_identity row as read from the map (feature 004; 056 §1.1 map).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The map's identity: the GUID minted at creation, the stored schema version and the creation time.
''' </summary>
Public Class MapIdentityRecord

    ''' <summary>The map GUID.</summary>
    Public Property MapGuid As String

    ''' <summary>The stored schema version.</summary>
    Public Property SchemaVersion As Integer

    ''' <summary>ISO-8601 UTC creation time.</summary>
    Public Property CreatedUtc As String

End Class

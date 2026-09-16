' File: MapStatusEnvelope.vb
' Project: CodeMem.Bridging
' Description: The map_status result (contracts/tools.md §3.7).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' readAtUtc, the two paths, the bound count, the entries, the unbound and inactive rows.
''' </summary>
Public Class MapStatusEnvelope

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>The map path.</summary>
    Public Property MapPath As String

    ''' <summary>The store path.</summary>
    Public Property StorePath As String

    ''' <summary>Active, bound rows.</summary>
    Public Property Bound As Integer

    ''' <summary>One entry per bound row.</summary>
    Public Property Entries As List(Of MapStatusEntryEnvelope)

    ''' <summary>Active rows with no binding.</summary>
    Public Property Unbound As List(Of UnboundEntryEnvelope)

    ''' <summary>Rows not active.</summary>
    Public Property Inactive As List(Of InactiveEntryEnvelope)

End Class

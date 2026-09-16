' File: OrphansEnvelope.vb
' Project: CodeMem.Bridging
' Description: The orphans result (060 §3.1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' readAtUtc, scope, filters, notExamined, total, byProject, byKind, orphans uncapped.
''' </summary>
Public Class OrphansEnvelope

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>The scope.</summary>
    Public Property Scope As ScopeEnvelope

    ''' <summary>The filters.</summary>
    Public Property Filters As OrphanFiltersEnvelope

    ''' <summary>The kinds never examined.</summary>
    Public Property NotExamined As List(Of String)

    ''' <summary>The number of orphans.</summary>
    Public Property Total As Integer

    ''' <summary>Counts per active project in scope.</summary>
    Public Property ByProject As List(Of ProjectCountEnvelope)

    ''' <summary>Counts per examined kind (every kind unfiltered; the one kind under a kind filter).</summary>
    Public Property ByKind As Dictionary(Of String, Integer)

    ''' <summary>Every orphan.</summary>
    Public Property Orphans As List(Of OrphanRowEnvelope)

End Class

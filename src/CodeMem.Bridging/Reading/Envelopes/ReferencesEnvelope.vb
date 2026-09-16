' File: ReferencesEnvelope.vb
' Project: CodeMem.Bridging
' Description: The references result (058 §3.4).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' readAtUtc, scope, symbol, count, occurrences uncapped.
''' </summary>
Public Class ReferencesEnvelope

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>The scope.</summary>
    Public Property Scope As ScopeEnvelope

    ''' <summary>The asked symbol.</summary>
    Public Property Symbol As ReferenceSymbolEnvelope

    ''' <summary>The number of occurrences.</summary>
    Public Property Count As Integer

    ''' <summary>Every occurrence.</summary>
    Public Property Occurrences As List(Of OccurrenceEnvelope)

End Class

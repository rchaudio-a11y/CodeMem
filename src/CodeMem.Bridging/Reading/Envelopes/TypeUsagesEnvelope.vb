' File: TypeUsagesEnvelope.vb
' Project: CodeMem.Bridging
' Description: The type_usages result (contracts/tools.md §3.6).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' readAtUtc, scope, symbol, byVerb, total, fromOutside, occurrences uncapped.
''' </summary>
Public Class TypeUsagesEnvelope

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>The scope.</summary>
    Public Property Scope As ScopeEnvelope

    ''' <summary>The asked type.</summary>
    Public Property Symbol As TypeSymbolEnvelope

    ''' <summary>Counts for the seven non-part_of verbs, zeros included.</summary>
    Public Property ByVerb As Dictionary(Of String, Integer)

    ''' <summary>Every occurrence.</summary>
    Public Property Total As Integer

    ''' <summary>The occurrences not from inside the type.</summary>
    Public Property FromOutside As Integer

    ''' <summary>The occurrences, by verb then location.</summary>
    Public Property Occurrences As List(Of TypeUsageOccurrenceEnvelope)

End Class

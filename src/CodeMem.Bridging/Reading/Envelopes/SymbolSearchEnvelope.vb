' File: SymbolSearchEnvelope.vb
' Project: CodeMem.Bridging
' Description: The symbol_search result (058 §3.2 with Declarations).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' readAtUtc, scope, filters, total, truncated, symbols.
''' </summary>
Public Class SymbolSearchEnvelope

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>The scope.</summary>
    Public Property Scope As ScopeEnvelope

    ''' <summary>The filters.</summary>
    Public Property Filters As SearchFiltersEnvelope

    ''' <summary>Declarations matched in the scope.</summary>
    Public Property Total As Integer

    ''' <summary>True when more than 200 declarations were narrowed.</summary>
    Public Property Truncated As Boolean

    ''' <summary>At most 200 declarations.</summary>
    Public Property Symbols As List(Of DeclarationEnvelope)

End Class

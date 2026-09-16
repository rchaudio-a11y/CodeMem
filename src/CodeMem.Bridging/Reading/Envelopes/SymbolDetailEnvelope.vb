' File: SymbolDetailEnvelope.vb
' Project: CodeMem.Bridging
' Description: The symbol_detail result (058 §3.3).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' readAtUtc, scope, symbol, parts, outbound and inbound under all eight verbs.
''' </summary>
Public Class SymbolDetailEnvelope

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>The scope.</summary>
    Public Property Scope As ScopeEnvelope

    ''' <summary>The symbol header.</summary>
    Public Property Symbol As DetailSymbolEnvelope

    ''' <summary>The declaring parts.</summary>
    Public Property Parts As List(Of PartEnvelope)

    ''' <summary>Outbound edges by verb, all eight keys.</summary>
    Public Property Outbound As Dictionary(Of String, List(Of OutboundEdgeEnvelope))

    ''' <summary>Inbound edges by verb, all eight keys.</summary>
    Public Property Inbound As Dictionary(Of String, List(Of InboundEdgeEnvelope))

End Class

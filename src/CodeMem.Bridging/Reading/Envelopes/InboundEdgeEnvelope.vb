' File: InboundEdgeEnvelope.vb
' Project: CodeMem.Bridging
' Description: One inbound edge of symbol_detail (058 §3.3): the source and the occurrence location.
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Every source is a mapped symbol.
''' </summary>
Public Class InboundEdgeEnvelope

    ''' <summary>The source's id.</summary>
    Public Property SourceSymbolId As Long

    ''' <summary>The source's name.</summary>
    Public Property SourceName As String

    ''' <summary>The source's kind text.</summary>
    Public Property SourceKind As String

    ''' <summary>The occurrence path.</summary>
    Public Property Path As String

    ''' <summary>The occurrence line.</summary>
    Public Property Line As Integer

    ''' <summary>The occurrence column.</summary>
    Public Property Column As Integer

End Class

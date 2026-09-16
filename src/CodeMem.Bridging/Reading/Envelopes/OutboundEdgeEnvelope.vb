' File: OutboundEdgeEnvelope.vb
' Project: CodeMem.Bridging
' Description: One outbound edge of symbol_detail (058 §3.3): the target, external or mapped, and the occurrence location.
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' targetSymbolId null with external true is a framework or package target.
''' </summary>
Public Class OutboundEdgeEnvelope

    ''' <summary>The target's id, or null when external.</summary>
    Public Property TargetSymbolId As Long?

    ''' <summary>The target's doc-comment id.</summary>
    Public Property TargetDocCommentId As String

    ''' <summary>The target's name, or null when external.</summary>
    Public Property TargetName As String

    ''' <summary>True when the target is outside the map.</summary>
    Public Property External As Boolean

    ''' <summary>The WithEvents member of a handles edge, or null.</summary>
    Public Property Via As NamedRefEnvelope

    ''' <summary>The occurrence path.</summary>
    Public Property Path As String

    ''' <summary>The occurrence line.</summary>
    Public Property Line As Integer

    ''' <summary>The occurrence column.</summary>
    Public Property Column As Integer

End Class

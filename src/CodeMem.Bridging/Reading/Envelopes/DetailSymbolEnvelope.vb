' File: DetailSymbolEnvelope.vb
' Project: CodeMem.Bridging
' Description: The symbol header of symbol_detail (058 §3.3 plus compiledInto, the requested id first).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The requested symbol's labels and every twin.
''' </summary>
Public Class DetailSymbolEnvelope

    ''' <summary>The solution id.</summary>
    Public Property SolutionId As Long

    ''' <summary>The solution key.</summary>
    Public Property SolutionKey As String

    ''' <summary>The symbol id.</summary>
    Public Property Id As Long

    ''' <summary>The doc-comment id.</summary>
    Public Property DocCommentId As String

    ''' <summary>The kind text.</summary>
    Public Property Kind As String

    ''' <summary>The name.</summary>
    Public Property Name As String

    ''' <summary>The container, or null.</summary>
    Public Property Container As NamedRefEnvelope

    ''' <summary>The declaration path.</summary>
    Public Property Path As String

    ''' <summary>The declaration line.</summary>
    Public Property Line As Integer

    ''' <summary>The declaration column.</summary>
    Public Property Column As Integer

    ''' <summary>Every project the declaration is compiled into, the requested id first.</summary>
    Public Property CompiledInto As List(Of CompiledIntoEnvelope)

End Class

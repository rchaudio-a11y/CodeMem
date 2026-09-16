' File: ReferenceSymbolEnvelope.vb
' Project: CodeMem.Bridging
' Description: The symbol header of references (058 §3.4).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Six labels of the asked symbol.
''' </summary>
Public Class ReferenceSymbolEnvelope

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

End Class

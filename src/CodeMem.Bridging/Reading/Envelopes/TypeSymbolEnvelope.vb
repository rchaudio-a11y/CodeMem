' File: TypeSymbolEnvelope.vb
' Project: CodeMem.Bridging
' Description: The symbol header of type_usages (contracts/tools.md §3.6).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Eight labels of the asked type.
''' </summary>
Public Class TypeSymbolEnvelope

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

    ''' <summary>The declaration path.</summary>
    Public Property Path As String

    ''' <summary>The declaration line.</summary>
    Public Property Line As Integer

End Class

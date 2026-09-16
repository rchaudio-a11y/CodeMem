' File: OrphanRowEnvelope.vb
' Project: CodeMem.Bridging
' Description: One orphan (060 §3.1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The symbol, its container and its declaring project.
''' </summary>
Public Class OrphanRowEnvelope

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

    ''' <summary>The declaring project, or null on an unlawful map.</summary>
    Public Property Project As NamedRefEnvelope

End Class

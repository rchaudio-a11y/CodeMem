' File: OrphanRecord.vb
' Project: CodeMem.Core
' Description: One orphan row as the bridge reads it: the symbol, its container and its declaring project (feature 004; 060 §3.1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' An active symbol of an examined kind that no recorded reference reaches, with the labels the result presents.
''' </summary>
Public Class OrphanRecord

    ''' <summary>Owning solution.</summary>
    Public Property SolutionId As Long

    ''' <summary>The owning solution's key.</summary>
    Public Property SolutionKey As String

    ''' <summary>The row id.</summary>
    Public Property Id As Long

    ''' <summary>The doc-comment id.</summary>
    Public Property DocCommentId As String

    ''' <summary>The kind text.</summary>
    Public Property Kind As String

    ''' <summary>Surface name.</summary>
    Public Property Name As String

    ''' <summary>Container row id, or Nothing.</summary>
    Public Property ContainerId As Long?

    ''' <summary>Container name, or Nothing.</summary>
    Public Property ContainerName As String

    ''' <summary>Primary declaration path.</summary>
    Public Property Path As String

    ''' <summary>Primary declaration start line.</summary>
    Public Property StartLine As Integer

    ''' <summary>Declaring project row id, or Nothing on an unlawful map.</summary>
    Public Property ProjectSymbolId As Long?

    ''' <summary>Declaring project name, or Nothing.</summary>
    Public Property ProjectName As String

End Class

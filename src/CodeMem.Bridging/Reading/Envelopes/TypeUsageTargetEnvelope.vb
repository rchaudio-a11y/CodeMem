' File: TypeUsageTargetEnvelope.vb
' Project: CodeMem.Bridging
' Description: What an occurrence of type_usages actually targets: the type, a constructor or a member, always mapped (contracts/tools.md §3.6).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The identity rule: a constructor call is never presented as a reference to the type.
''' </summary>
Public Class TypeUsageTargetEnvelope

    ''' <summary>The target's id.</summary>
    Public Property Id As Long

    ''' <summary>The target's doc-comment id.</summary>
    Public Property DocCommentId As String

    ''' <summary>The target's name.</summary>
    Public Property Name As String

    ''' <summary>The target's kind text.</summary>
    Public Property Kind As String

    ''' <summary>type, constructor or member.</summary>
    Public Property Role As String

    ''' <summary>Always false here: every target is a row of the map.</summary>
    Public Property External As Boolean

End Class

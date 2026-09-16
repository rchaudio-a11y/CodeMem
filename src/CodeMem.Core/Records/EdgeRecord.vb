' File: EdgeRecord.vb
' Project: CodeMem.Core
' Description: One code_edges row as the bridge reads it: every column plus the joined source, target and via names and kinds (feature 004).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' An occurrence for presentation. Target labels are Nothing when the target is external (TargetSymbolId Nothing).
''' </summary>
Public Class EdgeRecord

    ''' <summary>The edge id.</summary>
    Public Property Id As Long

    ''' <summary>Owning solution.</summary>
    Public Property SolutionId As Long

    ''' <summary>The source symbol's row id.</summary>
    Public Property SourceSymbolId As Long

    ''' <summary>The source symbol's name (joined).</summary>
    Public Property SourceName As String

    ''' <summary>The source symbol's kind text (joined).</summary>
    Public Property SourceKind As String

    ''' <summary>The verb text.</summary>
    Public Property Verb As String

    ''' <summary>The target's row id, or Nothing when external.</summary>
    Public Property TargetSymbolId As Long?

    ''' <summary>The target's doc-comment id, always present.</summary>
    Public Property TargetDocCommentId As String

    ''' <summary>The target's name (joined), or Nothing.</summary>
    Public Property TargetName As String

    ''' <summary>The target's kind text (joined), or Nothing.</summary>
    Public Property TargetKind As String

    ''' <summary>The WithEvents member of a handles edge, or Nothing.</summary>
    Public Property ViaSymbolId As Long?

    ''' <summary>The via member's name (joined), or Nothing.</summary>
    Public Property ViaName As String

    ''' <summary>Occurrence path, solution-relative.</summary>
    Public Property Path As String

    ''' <summary>Occurrence start offset.</summary>
    Public Property StartOffset As Integer

    ''' <summary>Occurrence length.</summary>
    Public Property Length As Integer

    ''' <summary>Occurrence start line.</summary>
    Public Property StartLine As Integer

    ''' <summary>Occurrence start column.</summary>
    Public Property StartColumn As Integer

    ''' <summary>type_usages only: true when the source is the asked type or lies in its containment subtree (research R51).</summary>
    Public Property FromInside As Boolean

    ''' <summary>type_usages only: what the edge actually targets - type, constructor or member; Nothing on other reads.</summary>
    Public Property TargetRole As String

End Class

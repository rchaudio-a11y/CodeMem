' File: EdgeRow.vb
' Project: CodeMem.Tests
' Description: A code_edges row as read by the test queries, with doc-comment ids joined in.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' One <c>code_edges</c> row for assertions.
''' </summary>
Public Class EdgeRow

    ''' <summary>Row id.</summary>
    Public Property Id As Long

    ''' <summary>Source symbol row id.</summary>
    Public Property SourceSymbolId As Long

    ''' <summary>Source symbol doc-comment id.</summary>
    Public Property SourceDocCommentId As String

    ''' <summary>Verb text.</summary>
    Public Property Verb As String

    ''' <summary>Target symbol row id or Nothing.</summary>
    Public Property TargetSymbolId As Long?

    ''' <summary>Target doc-comment id (always present).</summary>
    Public Property TargetDocCommentId As String

    ''' <summary>Via symbol row id or Nothing.</summary>
    Public Property ViaSymbolId As Long?

    ''' <summary>Via symbol doc-comment id or Nothing.</summary>
    Public Property ViaDocCommentId As String

    ''' <summary>Occurrence path.</summary>
    Public Property Path As String

    ''' <summary>Occurrence start offset.</summary>
    Public Property StartOffset As Integer

    ''' <summary>Occurrence length.</summary>
    Public Property Length As Integer

    ''' <summary>Occurrence start line.</summary>
    Public Property StartLine As Integer

    ''' <summary>Occurrence start column.</summary>
    Public Property StartColumn As Integer

End Class

' File: PartRow.vb
' Project: CodeMem.Tests
' Description: A code_parts row as read by the test queries, with its symbol's doc-comment id.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' One <c>code_parts</c> row for assertions.
''' </summary>
Public Class PartRow

    ''' <summary>Row id.</summary>
    Public Property Id As Long

    ''' <summary>Owning symbol row id.</summary>
    Public Property SymbolId As Long

    ''' <summary>Owning symbol doc-comment id.</summary>
    Public Property SymbolDocCommentId As String

    ''' <summary>Part path.</summary>
    Public Property Path As String

    ''' <summary>Part start offset.</summary>
    Public Property StartOffset As Integer

    ''' <summary>Part length.</summary>
    Public Property Length As Integer

    ''' <summary>Part start line.</summary>
    Public Property StartLine As Integer

    ''' <summary>Part start column.</summary>
    Public Property StartColumn As Integer

    ''' <summary>Part hash.</summary>
    Public Property PartHash As String

End Class

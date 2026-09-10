' File: SymbolRow.vb
' Project: CodeMem.Tests
' Description: A code_symbols row as read by the test queries.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' One <c>code_symbols</c> row for assertions.
''' </summary>
Public Class SymbolRow

    ''' <summary>Row id.</summary>
    Public Property Id As Long

    ''' <summary>Doc-comment id.</summary>
    Public Property DocCommentId As String

    ''' <summary>Kind text.</summary>
    Public Property Kind As String

    ''' <summary>Surface name.</summary>
    Public Property Name As String

    ''' <summary>Container row id or Nothing.</summary>
    Public Property ContainerId As Long?

    ''' <summary>Declaring project row id or Nothing.</summary>
    Public Property ProjectSymbolId As Long?

    ''' <summary>Primary declaration path.</summary>
    Public Property Path As String

    ''' <summary>Primary declaration start offset.</summary>
    Public Property StartOffset As Integer

    ''' <summary>Primary declaration length.</summary>
    Public Property Length As Integer

    ''' <summary>Primary declaration start line.</summary>
    Public Property StartLine As Integer

    ''' <summary>Primary declaration start column.</summary>
    Public Property StartColumn As Integer

    ''' <summary>Body hash.</summary>
    Public Property BodyHash As String

    ''' <summary>Active flag.</summary>
    Public Property IsActive As Boolean

    ''' <summary>First run that observed the row.</summary>
    Public Property FirstSeenRunId As Long

    ''' <summary>Last run that observed the row.</summary>
    Public Property LastSeenRunId As Long

End Class

' File: CandidateRow.vb
' Project: CodeMem.Tests
' Description: A rename_candidates row as read by the test queries.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' One <c>rename_candidates</c> row for assertions.
''' </summary>
Public Class CandidateRow

    ''' <summary>Row id.</summary>
    Public Property Id As Long

    ''' <summary>Run that wrote the candidate.</summary>
    Public Property RunId As Long

    ''' <summary>Retired symbol row id.</summary>
    Public Property RetiredSymbolId As Long

    ''' <summary>New symbol row id.</summary>
    Public Property NewSymbolId As Long

    ''' <summary>The equal body hash.</summary>
    Public Property BodyHash As String

    ''' <summary>Same-path evidence.</summary>
    Public Property SamePath As Boolean

    ''' <summary>Offset distance or Nothing.</summary>
    Public Property OffsetDistance As Integer?

    ''' <summary>Rank, 1 = nearest.</summary>
    Public Property Rank As Integer

End Class

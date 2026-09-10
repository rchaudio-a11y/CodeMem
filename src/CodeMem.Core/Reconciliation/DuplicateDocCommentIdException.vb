' File: DuplicateDocCommentIdException.vb
' Project: CodeMem.Core
' Description: Raised when two staged symbols share a doc-comment id; the run refuses before reconciliation (exit 1).
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' Two observed symbols carry one identity. Not a residual: the run refuses rather than guess which row is which.
''' </summary>
Public Class DuplicateDocCommentIdException
    Inherits Exception

    ''' <summary>The shared doc-comment id.</summary>
    Public ReadOnly Property DocCommentId As String

    ''' <summary>Where the first symbol is declared.</summary>
    Public ReadOnly Property First As SourceLocation

    ''' <summary>Where the second symbol is declared.</summary>
    Public ReadOnly Property Second As SourceLocation

    ''' <summary>
    ''' Creates the exception carrying both locations.
    ''' </summary>
    ''' <param name="docCommentId">The shared id.</param>
    ''' <param name="first">First declaration.</param>
    ''' <param name="second">Second declaration.</param>
    Public Sub New(docCommentId As String, first As SourceLocation, second As SourceLocation)
        MyBase.New("duplicate doc-comment id " & docCommentId & " at " & first.Path & ":" & first.StartLine & "," & first.StartColumn &
                   " and " & second.Path & ":" & second.StartLine & "," & second.StartColumn)
        Me.DocCommentId = docCommentId
        Me.First = first
        Me.Second = second
    End Sub

End Class

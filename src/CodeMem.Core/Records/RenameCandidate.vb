' File: RenameCandidate.vb
' Project: CodeMem.Core
' Description: A rename candidate proposal (Article VI (B)/(C)) staged before publication.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' A proposal that a new symbol is the renamed form of a row retired this run. Never applied by the extractor.
''' </summary>
Public Class RenameCandidate

    ''' <summary>The retired row.</summary>
    Public Property RetiredSymbolId As Long

    ''' <summary>Doc-comment id of the new symbol; resolved to its id at publication.</summary>
    Public Property NewDocCommentId As String

    ''' <summary>The equal body hash: the (B) evidence.</summary>
    Public Property BodyHash As String

    ''' <summary>True when both primary declarations lie in one file.</summary>
    Public Property SamePath As Boolean

    ''' <summary>Absolute offset difference of the primary declarations when same path; else Nothing.</summary>
    Public Property OffsetDistance As Integer?

    ''' <summary>1 = nearest; 1 when unique.</summary>
    Public Property Rank As Integer

End Class

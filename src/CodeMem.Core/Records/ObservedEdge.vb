' File: ObservedEdge.vb
' Project: CodeMem.Core
' Description: A relationship occurrence as observed by one run, staged in memory.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' A staged edge. Symbol ids are resolved from doc-comment ids at publication; a target or via with no row becomes a null id.
''' </summary>
Public Class ObservedEdge

    ''' <summary>Doc-comment id of the source row symbol.</summary>
    Public Property SourceDocCommentId As String

    ''' <summary>The verb.</summary>
    Public Property Verb As EdgeVerb

    ''' <summary>Doc-comment id of the target (always present, FR-015).</summary>
    Public Property TargetDocCommentId As String

    ''' <summary>Doc-comment id of the WithEvents member for a Handles item; otherwise Nothing.</summary>
    Public Property ViaDocCommentId As String

    ''' <summary>Where the occurrence is: the identifier that names the target (FR-018).</summary>
    Public Property Location As SourceLocation

End Class

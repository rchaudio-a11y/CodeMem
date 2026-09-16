' File: TypeUsageOccurrenceEnvelope.vb
' Project: CodeMem.Bridging
' Description: One occurrence of type_usages: verb, location, source, the actual target, fromInside, the edge id (contracts/tools.md §3.6; DUP1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The flat list's element.
''' </summary>
Public Class TypeUsageOccurrenceEnvelope

    ''' <summary>The verb text.</summary>
    Public Property Verb As String

    ''' <summary>The occurrence path.</summary>
    Public Property Path As String

    ''' <summary>The occurrence line.</summary>
    Public Property Line As Integer

    ''' <summary>The occurrence column.</summary>
    Public Property Column As Integer

    ''' <summary>The symbol the occurrence occurs in.</summary>
    Public Property Source As SourceRefEnvelope

    ''' <summary>The symbol it actually targets.</summary>
    Public Property Target As TypeUsageTargetEnvelope

    ''' <summary>True when the source is the type or lies inside it.</summary>
    Public Property FromInside As Boolean

    ''' <summary>The map's edge id: every edge appears once.</summary>
    Public Property EdgeId As Long

End Class

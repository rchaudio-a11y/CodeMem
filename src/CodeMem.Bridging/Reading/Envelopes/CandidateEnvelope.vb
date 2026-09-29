' File: CandidateEnvelope.vb
' Project: CodeMem.Bridging
' Description: One rename candidate (feature 006, T011; data-model §2; FR-502): the run, both sides with bare ids, and the proximity evidence.
' Author: RCH Automation LLC
' Created: 2026-09-29

''' <summary>
''' One row is one candidate: twins are never folded (spec Q1).
''' </summary>
Public Class CandidateEnvelope

    ''' <summary>The run that wrote it.</summary>
    Public Property RunId As Long

    ''' <summary>The retired side.</summary>
    Public Property Retired As CandidateSideEnvelope

    ''' <summary>The new side (serialised as new).</summary>
    Public Property [New] As CandidateSideEnvelope

    ''' <summary>Whether both primary declarations are in the same file.</summary>
    Public Property SamePath As Boolean

    ''' <summary>The distance between the start offsets; null when samePath is false.</summary>
    Public Property OffsetDistance As Integer?

    ''' <summary>The proximity rank; 1 is nearest.</summary>
    Public Property Rank As Integer

End Class

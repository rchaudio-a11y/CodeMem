' File: OccurrenceEnvelope.vb
' Project: CodeMem.Bridging
' Description: One occurrence of references (058 §3.4): verb, location, source.
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The target is the asked symbol.
''' </summary>
Public Class OccurrenceEnvelope

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

End Class

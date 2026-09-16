' File: OrphanFiltersEnvelope.vb
' Project: CodeMem.Bridging
' Description: The filters orphans echoes (060 §3.1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' kind and projectSymbolId as given, null where absent.
''' </summary>
Public Class OrphanFiltersEnvelope

    ''' <summary>The kind filter.</summary>
    Public Property Kind As String

    ''' <summary>The project row filter.</summary>
    Public Property ProjectSymbolId As Long?

End Class

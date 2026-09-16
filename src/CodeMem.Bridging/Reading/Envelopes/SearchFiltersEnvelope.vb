' File: SearchFiltersEnvelope.vb
' Project: CodeMem.Bridging
' Description: The filters symbol_search echoes (058 §3.2).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' name, kind and projectSymbolId as given, null where absent.
''' </summary>
Public Class SearchFiltersEnvelope

    ''' <summary>The name filter.</summary>
    Public Property Name As String

    ''' <summary>The kind filter.</summary>
    Public Property Kind As String

    ''' <summary>The project row filter.</summary>
    Public Property ProjectSymbolId As Long?

End Class

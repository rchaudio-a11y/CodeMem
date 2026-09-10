' File: SchemaVersion.vb
' Project: CodeMem.Core
' Description: The schema version this build of Core creates and accepts.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' The one schema version constant (research R12). Written to map_identity at creation and to every run; mismatch on open refuses.
''' </summary>
Public Module SchemaVersion

    ''' <summary>The current schema version.</summary>
    Public Const Current As Integer = 1

End Module

' File: SchemaVersion.vb
' Project: CodeMem.Core
' Description: The schema version this build of Core creates and accepts.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002): Current = 2 (sdk_version column and its triggers; a version-1 map is upgraded in place).
' 2026-09-17 (feature 005, T009): Current = 3 (extract_run_warnings; a version-1 or version-2 map is upgraded in place, research R63).

''' <summary>
''' The one schema version constant (research R12). Written to map_identity at creation or upgrade and to every run; a version other than 1, 2 or Current refuses (FR-114).
''' </summary>
Public Module SchemaVersion

    ''' <summary>The current schema version.</summary>
    Public Const Current As Integer = 3

End Module

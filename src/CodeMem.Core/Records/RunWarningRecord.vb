' File: RunWarningRecord.vb
' Project: CodeMem.Core
' Description: One row of extract_run_warnings: a NuGet restore warning the loader classified through the project's assets log and the run recorded (005 FR-427, FR-429; schema version 3; data-model §11).
' Author: RCH Automation LLC
' Created: 2026-09-17

''' <summary>
''' ProjectPath is solution-relative once the run has written it; the loader hands the full path and the run makes it relative.
''' </summary>
Public Class RunWarningRecord

    ''' <summary>The row id; 0 before the insert.</summary>
    Public Property Id As Long

    ''' <summary>The run the warning belongs to; 0 before the insert.</summary>
    Public Property RunId As Long

    ''' <summary>NuGet's code, e.g. NU1701.</summary>
    Public Property Code As String

    ''' <summary>The project file the warning names, solution-relative; Nothing when it names none.</summary>
    Public Property ProjectPath As String

    ''' <summary>NuGet's message as the assets log records it.</summary>
    Public Property Message As String

End Class

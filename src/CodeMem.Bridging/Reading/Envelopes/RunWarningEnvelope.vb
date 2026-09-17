' File: RunWarningEnvelope.vb
' Project: CodeMem.Bridging
' Description: One warning of an extract run on the wire: NuGet's code, the project file it names (solution-relative, or null) and the message (005 FR-430; contracts/tools.md §3.8).
' Author: RCH Automation LLC
' Created: 2026-09-17

''' <summary>
''' Listed under run.warnings by extract; counted under latestRun.warnings by solutions.
''' </summary>
Public Class RunWarningEnvelope

    ''' <summary>NuGet's code, e.g. NU1701.</summary>
    Public Property Code As String

    ''' <summary>The project file the warning names, solution-relative; null when it names none.</summary>
    Public Property ProjectPath As String

    ''' <summary>NuGet's message.</summary>
    Public Property Message As String

End Class

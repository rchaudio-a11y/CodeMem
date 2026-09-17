' File: AssetsLogMatch.vb
' Project: CodeMem.Extraction
' Description: What AssetsLog.Classify found for one workspace failure: NuGet's code and level from the project's assets log, the project file the failure named and the message (005 FR-427; research R62; data-model §11).
' Author: RCH Automation LLC
' Created: 2026-09-17

''' <summary>
''' A record, never a rule: the rule lives in <see cref="AssetsLog"/> and the decision in SolutionLoader.
''' </summary>
Public Class AssetsLogMatch

    ''' <summary>NuGet's code, e.g. NU1701 or NU1101.</summary>
    Public Property Code As String

    ''' <summary>NuGet's level: Warning or Error.</summary>
    Public Property Level As String

    ''' <summary>The full path of the project file the failure named.</summary>
    Public Property ProjectPath As String

    ''' <summary>The message, as the failure carried it and the assets log records it.</summary>
    Public Property Message As String

End Class

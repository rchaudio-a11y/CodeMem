' File: LogOutcome.vb
' Project: CodeMem.Bridging
' Description: Whether an extract.log line was written, and why not (FR-351: a line that cannot be written never stops the extraction).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Returned by <see cref="ExtractLog.Append"/>; carried into the result as logged and logError.
''' </summary>
Public Class LogOutcome

    ''' <summary>True when the line was appended.</summary>
    Public Property Logged As Boolean

    ''' <summary>The failure's message, or Nothing.</summary>
    Public Property Reason As String

End Class

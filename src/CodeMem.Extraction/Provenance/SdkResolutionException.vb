' File: SdkResolutionException.vb
' Project: CodeMem.Extraction
' Description: Raised when no .NET host can be found or no SDK resolves for the solution directory; the run refuses with exit 1 (FR-109, research R21).
' Author: RCH Automation LLC
' Created: 2026-09-10

''' <summary>
''' The SDK version stamp could not be read. The run never substitutes a value (spec Assumptions: a stamp that cannot say what
''' compiled is worse than no run); the message carries the host's own text, folded to one line.
''' </summary>
Public Class SdkResolutionException
    Inherits Exception

    ''' <summary>
    ''' Creates the exception.
    ''' </summary>
    ''' <param name="message">Why no SDK version resolved.</param>
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

End Class

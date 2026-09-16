' File: SolutionsEnvelope.vb
' Project: CodeMem.Bridging
' Description: The solutions result (056 §1.1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' map, mapPath, readAtUtc, solutions ordered by key.
''' </summary>
Public Class SolutionsEnvelope

    ''' <summary>The map identity.</summary>
    Public Property Map As MapIdentityEnvelope

    ''' <summary>The map path that answered.</summary>
    Public Property MapPath As String

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>Every solution.</summary>
    Public Property Solutions As List(Of SolutionEnvelope)

End Class

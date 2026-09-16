' File: StaleEntryEnvelope.vb
' Project: CodeMem.Bridging
' Description: One bound solution considered by extract(stale): its verdict, whether it was extracted, and the result (contracts/tools.md §3.8).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' result is null when the solution was skipped.
''' </summary>
Public Class StaleEntryEnvelope

    ''' <summary>The registry key.</summary>
    Public Property SolutionKey As String

    ''' <summary>The map_status verdict.</summary>
    Public Property Verdict As String

    ''' <summary>True when an extraction ran.</summary>
    Public Property Extracted As Boolean

    ''' <summary>The extract result, or null.</summary>
    Public Property Result As ExtractResult

End Class

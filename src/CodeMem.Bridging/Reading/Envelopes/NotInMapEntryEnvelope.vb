' File: NotInMapEntryEnvelope.vb
' Project: CodeMem.Bridging
' Description: One directory an extract refused as not in the map: when and by whom it was observed, the solution files it holds now, the suggested key and the command that would add it (005 data-model §4).
' Author: RCH Automation LLC
' Created: 2026-09-17

''' <summary>
''' Verdict not_in_map; listed by map_status until a mapped root contains the directory.
''' </summary>
Public Class NotInMapEntryEnvelope

    ''' <summary>The directory as the log recorded it.</summary>
    Public Property Path As String

    ''' <summary>Always not_in_map.</summary>
    Public Property Verdict As String

    ''' <summary>The most recent line's time.</summary>
    Public Property ObservedUtc As String

    ''' <summary>The most recent line's origin: tool, green_build or manual.</summary>
    Public Property Origin As String

    ''' <summary>The .sln and .slnx files the directory holds now, full paths; empty when none or unreadable.</summary>
    Public Property SolutionFiles As List(Of String)

    ''' <summary>The one file's name without extension, or null.</summary>
    Public Property SuggestedKey As String

    ''' <summary>extract --solution-key &lt;key&gt; --solution &lt;file&gt;, or null.</summary>
    Public Property Command As String

    ''' <summary>Why no key is suggested, or null.</summary>
    Public Property Note As String

End Class

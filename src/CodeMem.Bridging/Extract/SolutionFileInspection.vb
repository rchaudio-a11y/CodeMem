' File: SolutionFileInspection.vb
' Project: CodeMem.Bridging
' Description: What SolutionFileSuggestion.Inspect found in one directory: whether it could be read, its top-level solution files, the key and command suggested when there is exactly one, and ambiguity when there are more (005 FR-411; data-model §3; research R66).
' Author: RCH Automation LLC
' Created: 2026-09-17

''' <summary>
''' A record, never a rule: the rule lives in <see cref="SolutionFileSuggestion"/>.
''' </summary>
Public Class SolutionFileInspection

    ''' <summary>False when the directory does not exist or could not be listed; Files is then empty.</summary>
    Public Property Readable As Boolean

    ''' <summary>The full paths of the directory's own .sln files, then its .slnx files, each group in ordinal order.</summary>
    Public Property Files As List(Of String)

    ''' <summary>The file name without extension when exactly one file was found; otherwise Nothing.</summary>
    Public Property SuggestedKey As String

    ''' <summary>The command that adds the solution when exactly one file was found; otherwise Nothing.</summary>
    Public Property Command As String

    ''' <summary>True when more than one solution file was found; no key is suggested.</summary>
    Public Property Ambiguous As Boolean

End Class

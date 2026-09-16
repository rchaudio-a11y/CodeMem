' File: EdgeVerbs.vb
' Project: CodeMem.Bridging
' Description: The eight verb texts in the fixed order the results group by, and the seven that are not containment (058 §3.3; contracts/tools.md §3.6).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' Derived from Core's EdgeVerb enum, never typed twice.
''' </summary>
Public Module EdgeVerbs

    ''' <summary>The eight verbs in the fixed order: part_of, calls, uses, implements, extends, imports, depends_on, handles.</summary>
    Public ReadOnly All As String() = AllVerbs()

    ''' <summary>The seven verbs that are not part_of, in the same order.</summary>
    Public ReadOnly NonPartOf As String() = Array.FindAll(All, Function(verb As String) verb <> "part_of")

    Private Function AllVerbs() As String()
        Dim verbs As List(Of String) = New List(Of String)()
        For Each value As EdgeVerb In [Enum].GetValues(GetType(EdgeVerb))
            verbs.Add(EdgeVerbNames.ToText(value))
        Next
        Return verbs.ToArray()
    End Function

End Module

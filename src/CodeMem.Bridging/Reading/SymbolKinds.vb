' File: SymbolKinds.vb
' Project: CodeMem.Bridging
' Description: The fourteen kind texts in schema order, the twelve orphans examine, and the six type kinds type_usages accepts (FR-313, FR-319; 060 FR-405).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' Derived from Core's SymbolKind enum, never typed twice.
''' </summary>
Public Module SymbolKinds

    ''' <summary>The fourteen kinds, in schema order.</summary>
    Public ReadOnly All As String() = AllKinds()

    ''' <summary>The kinds orphans never examines.</summary>
    Public ReadOnly NotExamined As String() = New String() {"namespace", "project"}

    ''' <summary>The twelve kinds orphans examines, in schema order.</summary>
    Public ReadOnly Examined As String() = Array.FindAll(All, Function(kind As String) Array.IndexOf(NotExamined, kind) < 0)

    ''' <summary>The kinds type_usages accepts.</summary>
    Public ReadOnly TypeKinds As String() = New String() {"class", "module", "structure", "interface", "enum", "delegate"}

    ''' <summary>
    ''' Whether a text is one of the fourteen kinds.
    ''' </summary>
    ''' <param name="text">The text.</param>
    ''' <returns>True when known.</returns>
    Public Function IsKnown(text As String) As Boolean
        Return Array.IndexOf(All, text) >= 0
    End Function

    Private Function AllKinds() As String()
        Dim kinds As List(Of String) = New List(Of String)()
        For Each value As SymbolKind In [Enum].GetValues(GetType(SymbolKind))
            kinds.Add(SymbolKindNames.ToText(value))
        Next
        Return kinds.ToArray()
    End Function

End Module

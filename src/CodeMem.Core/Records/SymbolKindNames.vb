' File: SymbolKindNames.vb
' Project: CodeMem.Core
' Description: Maps SymbolKind values to and from the schema kind text, and gives the publication insert rank.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' The schema text of each <see cref="SymbolKind"/>.
''' </summary>
Public Module SymbolKindNames

    ''' <summary>
    ''' Returns the schema text for a kind.
    ''' </summary>
    ''' <param name="kind">The kind.</param>
    ''' <returns>The <c>code_symbols.kind</c> text.</returns>
    Public Function ToText(kind As SymbolKind) As String
        Select Case kind
            Case SymbolKind.Namespace_ : Return "namespace"
            Case SymbolKind.Class_ : Return "class"
            Case SymbolKind.Module_ : Return "module"
            Case SymbolKind.Structure_ : Return "structure"
            Case SymbolKind.Interface_ : Return "interface"
            Case SymbolKind.Enum_ : Return "enum"
            Case SymbolKind.EnumMember : Return "enum_member"
            Case SymbolKind.Delegate_ : Return "delegate"
            Case SymbolKind.Method : Return "method"
            Case SymbolKind.Constructor : Return "constructor"
            Case SymbolKind.Property_ : Return "property"
            Case SymbolKind.Field : Return "field"
            Case SymbolKind.Event_ : Return "event"
            Case SymbolKind.Project : Return "project"
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(kind), kind, "unknown symbol kind")
        End Select
    End Function

    ''' <summary>
    ''' Parses schema text back into a kind.
    ''' </summary>
    ''' <param name="text">The <c>code_symbols.kind</c> text.</param>
    ''' <returns>The kind.</returns>
    Public Function Parse(text As String) As SymbolKind
        For Each value As SymbolKind In [Enum].GetValues(GetType(SymbolKind))
            If String.Equals(ToText(value), text, StringComparison.Ordinal) Then
                Return value
            End If
        Next
        Throw New ArgumentOutOfRangeException(NameOf(text), text, "unknown symbol kind text")
    End Function

    ''' <summary>
    ''' Publication rank: projects before namespaces before types before members, so containers exist before their members are inserted.
    ''' </summary>
    ''' <param name="kind">The kind.</param>
    ''' <returns>0 for project, 1 for namespace, 2 for type kinds, 3 for members.</returns>
    Public Function InsertRank(kind As SymbolKind) As Integer
        Select Case kind
            Case SymbolKind.Project : Return 0
            Case SymbolKind.Namespace_ : Return 1
            Case SymbolKind.Class_, SymbolKind.Module_, SymbolKind.Structure_, SymbolKind.Interface_, SymbolKind.Enum_, SymbolKind.Delegate_ : Return 2
            Case Else : Return 3
        End Select
    End Function

End Module

' File: EdgeVerbNames.vb
' Project: CodeMem.Core
' Description: Maps EdgeVerb values to and from the schema verb text.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' The schema text of each <see cref="EdgeVerb"/>.
''' </summary>
Public Module EdgeVerbNames

    ''' <summary>
    ''' Returns the schema text for a verb.
    ''' </summary>
    ''' <param name="verb">The verb.</param>
    ''' <returns>The <c>code_edges.verb</c> text.</returns>
    Public Function ToText(verb As EdgeVerb) As String
        Select Case verb
            Case EdgeVerb.PartOf : Return "part_of"
            Case EdgeVerb.Calls : Return "calls"
            Case EdgeVerb.Uses : Return "uses"
            Case EdgeVerb.Implements_ : Return "implements"
            Case EdgeVerb.Extends : Return "extends"
            Case EdgeVerb.Imports_ : Return "imports"
            Case EdgeVerb.DependsOn : Return "depends_on"
            Case EdgeVerb.Handles_ : Return "handles"
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(verb), verb, "unknown edge verb")
        End Select
    End Function

    ''' <summary>
    ''' Parses schema text back into a verb.
    ''' </summary>
    ''' <param name="text">The <c>code_edges.verb</c> text.</param>
    ''' <returns>The verb.</returns>
    Public Function Parse(text As String) As EdgeVerb
        For Each value As EdgeVerb In [Enum].GetValues(GetType(EdgeVerb))
            If String.Equals(ToText(value), text, StringComparison.Ordinal) Then
                Return value
            End If
        Next
        Throw New ArgumentOutOfRangeException(NameOf(text), text, "unknown edge verb text")
    End Function

End Module

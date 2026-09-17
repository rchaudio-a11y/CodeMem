' File: RegistryTableMissingException.vb
' Project: CodeMem.Core
' Description: Core's typed signal that the store holds no code_map_solutions table, learned from the query itself (feature 004, INC1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Thrown by <c>CodeMapSolutionsRepository.ReadAll</c> when the driver answers "no such table": the registry migration has not gone live on
''' that store. The bridge turns it into the RegistryAbsent refusal naming the store path.
''' </summary>
Public Class RegistryTableMissingException
    Inherits Exception

    ''' <summary>
    ''' Wraps the driver's message.
    ''' </summary>
    ''' <param name="message">The message.</param>
    ''' <param name="inner">The driver exception.</param>
    Public Sub New(message As String, inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class

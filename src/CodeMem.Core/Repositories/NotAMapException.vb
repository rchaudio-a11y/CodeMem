' File: NotAMapException.vb
' Project: CodeMem.Core
' Description: Raised when the file at --db is a SQLite database that is not a CodeMem map; the run refuses and writes nothing (FR-105, Article IX).
' Author: RCH Automation LLC
' Created: 2026-09-10

''' <summary>
''' The file has user tables but no populated map_identity table. CodeMem opens no database in a role other than its own map,
''' so it is refused (exit 1) rather than repurposed or repaired.
''' </summary>
Public Class NotAMapException
    Inherits Exception

    ''' <summary>The path that was refused.</summary>
    Public ReadOnly Property Path As String

    ''' <summary>
    ''' Creates the exception with the message <c>not a map: &lt;path&gt;</c> (contracts/cli.md).
    ''' </summary>
    ''' <param name="path">The refused file path.</param>
    Public Sub New(path As String)
        MyBase.New("not a map: " & path)
        Me.Path = path
    End Sub

End Class

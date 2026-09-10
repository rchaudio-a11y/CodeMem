' File: SchemaVersionMismatchException.vb
' Project: CodeMem.Core
' Description: Raised when a map was created by a different schema version; the run refuses (exit 1).
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' The map's schema version differs from <see cref="SchemaVersion.Current"/>. Migration is not part of this feature.
''' </summary>
Public Class SchemaVersionMismatchException
    Inherits Exception

    ''' <summary>The version stored in the map.</summary>
    Public ReadOnly Property Found As Integer

    ''' <summary>The version this build expects.</summary>
    Public ReadOnly Property Expected As Integer

    ''' <summary>
    ''' Creates the exception.
    ''' </summary>
    ''' <param name="found">The stored version.</param>
    ''' <param name="expected">The expected version.</param>
    Public Sub New(found As Integer, expected As Integer)
        MyBase.New("schema version mismatch: map has " & found & ", extractor expects " & expected)
        Me.Found = found
        Me.Expected = expected
    End Sub

End Class

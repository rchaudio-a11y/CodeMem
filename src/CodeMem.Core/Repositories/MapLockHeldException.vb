' File: MapLockHeldException.vb
' Project: CodeMem.Core
' Description: Raised when the map's write lock is held by another extractor or cannot be taken (exit 3).
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' BEGIN IMMEDIATE was refused: another writer holds the map, or the file cannot be written. Nothing was written.
''' </summary>
Public Class MapLockHeldException
    Inherits Exception

    ''' <summary>
    ''' Creates the exception.
    ''' </summary>
    ''' <param name="message">Why the lock could not be taken.</param>
    ''' <param name="inner">The underlying database error.</param>
    Public Sub New(message As String, inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class

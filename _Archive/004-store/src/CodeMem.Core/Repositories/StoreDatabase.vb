' File: StoreDatabase.vb
' Project: CodeMem.Core
' Description: The one door to the MemOS store, read-only, for the code_map_solutions registry alone (constitution v1.3.0, Article IX's ruled exception; research R44).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports Microsoft.Data.Sqlite

''' <summary>
''' The second production site of New SqliteConnection (the Review Gate names both files). Opens memos.sqlite with Mode=ReadOnly, no pragma,
''' no table probe (INC1: the registry module learns a missing table from its own query). Never a map: no InspectSchema, no lock.
''' </summary>
Public Class StoreDatabase
    Implements IDisposable

    Private ReadOnly _connection As SqliteConnection

    Private Sub New(connection As SqliteConnection)
        _connection = connection
    End Sub

    ''' <summary>
    ''' Opens the store read-only: Mode=ReadOnly, no pooling, a 3 s busy timeout. A missing file fails with SQLITE_CANTOPEN and is not
    ''' created; on any failure the connection is disposed and the exception rethrown.
    ''' </summary>
    ''' <param name="path">The store file path, typed so ';' and '=' in it are literal.</param>
    ''' <returns>The open, read-only store.</returns>
    Public Shared Function OpenReadOnly(path As String) As StoreDatabase
        Dim builder As SqliteConnectionStringBuilder = New SqliteConnectionStringBuilder With {.DataSource = path, .Mode = SqliteOpenMode.ReadOnly, .Pooling = False, .DefaultTimeout = 3}
        Dim connection As SqliteConnection = New SqliteConnection(builder.ConnectionString)
        Try
            connection.Open()
        Catch
            connection.Dispose()
            Throw
        End Try
        Return New StoreDatabase(connection)
    End Function

    ''' <summary>
    ''' Creates a command on the open connection for the registry module.
    ''' </summary>
    ''' <returns>A new command.</returns>
    Public Function CreateCommand() As SqliteCommand
        Return _connection.CreateCommand()
    End Function

    ''' <summary>
    ''' Closes the connection.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        _connection.Dispose()
    End Sub

End Class

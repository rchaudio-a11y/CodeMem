' File: MapDatabase.vb
' Project: CodeMem.Core
' Description: Opens or creates the map file and owns the one write transaction that doubles as the extractor lock (research R6).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO
Imports Microsoft.Data.Sqlite

''' <summary>
''' The only place a SqliteConnection is created in production (FR-003). Holds the connection for one run.
''' </summary>
Public Class MapDatabase
    Implements IDisposable

    Private ReadOnly _connection As SqliteConnection
    Private _inTransaction As Boolean

    Private Sub New(connection As SqliteConnection)
        _connection = connection
    End Sub

    ''' <summary>
    ''' Opens the map at a path, creating the file, its schema and its identity row when absent; refuses a schema-version mismatch.
    ''' </summary>
    ''' <param name="path">The map file path; its directory must exist.</param>
    ''' <returns>The open database.</returns>
    Public Shared Function OpenOrCreate(path As String) As MapDatabase
        Dim fresh As Boolean = Not File.Exists(path)
        Dim connection As SqliteConnection = New SqliteConnection("Data Source=" & path & ";Pooling=False")
        connection.Open()
        Try
            Using pragma As SqliteCommand = connection.CreateCommand()
                pragma.CommandText = "PRAGMA foreign_keys = ON"
                pragma.ExecuteNonQuery()
            End Using
            If fresh Then
                SchemaRepository.CreateSchema(connection)
                MapIdentityRepository.Insert(connection, Guid.NewGuid().ToString(), SchemaVersion.Current, Timestamps.NowUtc())
            End If
            Dim found As Integer = SchemaRepository.ReadSchemaVersion(connection)
            If found <> SchemaVersion.Current Then
                Throw New SchemaVersionMismatchException(found, SchemaVersion.Current)
            End If
        Catch
            connection.Dispose()
            Throw
        End Try
        Return New MapDatabase(connection)
    End Function

    ''' <summary>
    ''' Creates a command on the open connection for a repository method.
    ''' </summary>
    ''' <returns>A new command.</returns>
    Public Function CreateCommand() As SqliteCommand
        Return _connection.CreateCommand()
    End Function

    ''' <summary>
    ''' Takes the write lock: BEGIN IMMEDIATE with a zero busy timeout. SQLITE_BUSY or SQLITE_READONLY becomes <see cref="MapLockHeldException"/> at once.
    ''' </summary>
    ''' <remarks>
    ''' Verified by I10 (research R6 said this must be tested, not assumed): a Microsoft.Data.Sqlite command timeout of zero means
    ''' "wait forever", and its smallest positive value is one second, which would break SC-008. The one statement that must not wait
    ''' is therefore issued on the driver's own native handle with the native busy timeout at zero; every other statement goes through
    ''' Microsoft.Data.Sqlite as usual.
    ''' </remarks>
    Public Sub BeginImmediate()
        Dim handle As SQLitePCL.sqlite3 = _connection.Handle
        SQLitePCL.raw.sqlite3_busy_timeout(handle, 0)
        Dim errorMessage As String = Nothing
        Dim rc As Integer = SQLitePCL.raw.sqlite3_exec(handle, "BEGIN IMMEDIATE", errorMessage)
        If rc = SQLitePCL.raw.SQLITE_BUSY OrElse rc = SQLitePCL.raw.SQLITE_READONLY Then
            Throw New MapLockHeldException("lock held: " & errorMessage, New SqliteException(errorMessage, rc))
        End If
        If rc <> SQLitePCL.raw.SQLITE_OK Then
            Throw New SqliteException("BEGIN IMMEDIATE failed: " & errorMessage, rc)
        End If
        _inTransaction = True
    End Sub

    ''' <summary>
    ''' Commits the open transaction.
    ''' </summary>
    Public Sub Commit()
        Using command As SqliteCommand = _connection.CreateCommand()
            command.CommandText = "COMMIT"
            command.ExecuteNonQuery()
        End Using
        _inTransaction = False
    End Sub

    ''' <summary>
    ''' Rolls back the open transaction; a no-op when none is open.
    ''' </summary>
    Public Sub Rollback()
        If Not _inTransaction Then Return
        Using command As SqliteCommand = _connection.CreateCommand()
            command.CommandText = "ROLLBACK"
            command.ExecuteNonQuery()
        End Using
        _inTransaction = False
    End Sub

    ''' <summary>
    ''' Rolls back anything still open and closes the connection.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            Rollback()
        Catch ex As SqliteException
            ' the connection is closing; a failed rollback leaves the journal for the next opener
        End Try
        _connection.Dispose()
    End Sub

End Class

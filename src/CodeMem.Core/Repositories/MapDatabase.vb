' File: MapDatabase.vb
' Project: CodeMem.Core
' Description: Opens the map file through a typed connection string and owns the one write transaction that doubles as the extractor lock (research R6, R23, R24).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002): OpenOrCreate became Open (no schema work; SqliteConnectionStringBuilder, F5) plus InspectSchema (F6, FR-105).
' Creation and the 1 -> 2 upgrade now happen in ExtractionRun inside BEGIN IMMEDIATE, so a race to create one path is serialized.
'
' 2026-09-15 (feature 004, T011): ReadOnlyConnectionString and OpenReadOnly - the bridge's door, Mode=ReadOnly, no pragma (research R44,
' FR-303) - and BeginRead / EndRead, one deferred read transaction per tool call (CON4, research R58). The extractor's path is unchanged.
'
' 2026-09-17 (feature 005, T009): InspectSchema returns Version2 for a 004-era map so ExtractionRun migrates it to 3 (research R63).

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
    ''' Opens the file at a path (created 0 bytes when absent) with the path as a typed connection-string value, so a path holding ';' or '='
    ''' opens exactly that file (FR-104). Sets PRAGMA foreign_keys = ON and PRAGMA journal_mode = DELETE (connection settings, not
    ''' transactional). Does no schema work: call <see cref="BeginImmediate"/> then <see cref="InspectSchema"/>.
    ''' </summary>
    ''' <param name="path">The map file path; its directory must exist.</param>
    ''' <returns>The open database.</returns>
    Public Shared Function Open(path As String) As MapDatabase
        Dim builder As SqliteConnectionStringBuilder = New SqliteConnectionStringBuilder With {.DataSource = path, .Pooling = False}
        Dim connection As SqliteConnection = New SqliteConnection(builder.ConnectionString)
        connection.Open()
        Try
            Using pragma As SqliteCommand = connection.CreateCommand()
                pragma.CommandText = "PRAGMA foreign_keys = ON"
                pragma.ExecuteNonQuery()
            End Using
            Using pragma As SqliteCommand = connection.CreateCommand()
                pragma.CommandText = "PRAGMA journal_mode = DELETE"
                pragma.ExecuteNonQuery()
            End Using
        Catch
            connection.Dispose()
            Throw
        End Try
        Return New MapDatabase(connection)
    End Function

    ''' <summary>
    ''' The read-only connection string (feature 004, research R44): Mode=ReadOnly, no pooling, a 3 s busy timeout (R53). A missing file
    ''' fails with SQLITE_CANTOPEN and is not created; a write fails with SQLITE_READONLY (demonstrated by B01's fire, FR-342).
    ''' </summary>
    ''' <param name="path">The map file path, typed so ';' and '=' in it are literal.</param>
    ''' <returns>The connection string.</returns>
    Public Shared Function ReadOnlyConnectionString(path As String) As String
        Dim builder As SqliteConnectionStringBuilder = New SqliteConnectionStringBuilder With {.DataSource = path, .Mode = SqliteOpenMode.ReadOnly, .Pooling = False, .DefaultTimeout = 3}
        Return builder.ConnectionString
    End Function

    ''' <summary>
    ''' Opens the map read-only for one bridge call (FR-303): no journal_mode pragma (a write on a read-only connection) and no foreign_keys
    ''' pragma (reads need none). <see cref="InspectSchema"/> works unchanged; <see cref="BeginImmediate"/> would refuse. On any failure the
    ''' connection is disposed and the exception rethrown.
    ''' </summary>
    ''' <param name="path">The map file path; must exist.</param>
    ''' <returns>The open, read-only database.</returns>
    Public Shared Function OpenReadOnly(path As String) As MapDatabase
        Dim connection As SqliteConnection = New SqliteConnection(ReadOnlyConnectionString(path))
        Try
            connection.Open()
        Catch
            connection.Dispose()
            Throw
        End Try
        Return New MapDatabase(connection)
    End Function

    ''' <summary>
    ''' Begins one deferred read transaction (an ordinary BEGIN; CON4, research R58): the SHARED lock lasts until <see cref="EndRead"/>, so a
    ''' publication cannot straddle a call's reads.
    ''' </summary>
    Public Sub BeginRead()
        Using command As SqliteCommand = _connection.CreateCommand()
            command.CommandText = "BEGIN"
            command.ExecuteNonQuery()
        End Using
        _inTransaction = True
    End Sub

    ''' <summary>
    ''' Ends the read transaction (COMMIT); a no-op when none is open.
    ''' </summary>
    Public Sub EndRead()
        If Not _inTransaction Then Return
        Commit()
    End Sub

    ''' <summary>
    ''' Classifies the file under the write lock (data-model.md "Map states at open"): no user table -> Fresh; no map_identity table or
    ''' no row -> Foreign; schema_version 1 -> Version1; 2 -> Version2 (feature 005); SchemaVersion.Current -> Current; anything else ->
    ''' Newer. One method, one door (Article XII). A file that is not SQLite fails on the first query with SQLITE_NOTADB.
    ''' </summary>
    ''' <param name="version">Receives the stored schema version, or 0 when the file is Fresh or Foreign.</param>
    ''' <returns>The state.</returns>
    Public Function InspectSchema(ByRef version As Integer) As SchemaState
        version = 0
        If SchemaRepository.CountUserTables(Me) = 0 Then Return SchemaState.Fresh
        If Not SchemaRepository.HasMapIdentityRow(Me) Then Return SchemaState.Foreign
        version = SchemaRepository.ReadSchemaVersion(Me)
        If version = 1 Then Return SchemaState.Version1
        If version = 2 Then Return SchemaState.Version2
        If version = SchemaVersion.Current Then Return SchemaState.Current
        Return SchemaState.Newer
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

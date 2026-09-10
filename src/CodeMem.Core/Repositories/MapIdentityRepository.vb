' File: MapIdentityRepository.vb
' Project: CodeMem.Core
' Description: The one-row map_identity table: written once at creation, read thereafter (Article IX, I14).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Microsoft.Data.Sqlite

''' <summary>
''' Inserts the identity row on a fresh file and reads the GUID.
''' </summary>
Public Module MapIdentityRepository

    ''' <summary>
    ''' Inserts the single identity row. Only ever called by <see cref="MapDatabase.OpenOrCreate"/> on a fresh file.
    ''' </summary>
    ''' <param name="connection">An open connection to the fresh file.</param>
    ''' <param name="guid">The minted GUID.</param>
    ''' <param name="schemaVersion">The schema version created.</param>
    ''' <param name="createdUtc">Creation timestamp.</param>
    Public Sub Insert(connection As SqliteConnection, guid As String, schemaVersion As Integer, createdUtc As String)
        Using command As SqliteCommand = connection.CreateCommand()
            command.CommandText = "INSERT INTO map_identity (id, map_guid, schema_version, created_utc) VALUES (1, @map_guid, @schema_version, @created_utc)"
            command.Parameters.AddWithValue("@map_guid", guid)
            command.Parameters.AddWithValue("@schema_version", schemaVersion)
            command.Parameters.AddWithValue("@created_utc", createdUtc)
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Reads the map GUID.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <returns>The GUID text.</returns>
    Public Function ReadGuid(db As MapDatabase) As String
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT map_guid FROM map_identity WHERE id = 1"
            Return CStr(command.ExecuteScalar())
        End Using
    End Function

End Module

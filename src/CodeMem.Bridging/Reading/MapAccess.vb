' File: MapAccess.vb
' Project: CodeMem.Bridging
' Description: The one door for a tool's map: open read-only, inspect against the pin, begin the call's read transaction; every map-level refusal by name (FR-303, FR-305, CON4; research R44, R53, R58).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO
Imports CodeMem.Core
Imports Microsoft.Data.Sqlite

''' <summary>
''' Order at the map stage: MapAbsent, then the driver's answer (Busy, Unopenable), then BEGIN, then NotAMap, then VersionBelow /
''' VersionAbove against <see cref="BridgeSchemaPin.Required"/>. BEGIN comes before the inspection so its first SELECT takes the SHARED
''' lock inside the read transaction, where it lasts until EndRead (B01 (6): a deferred BEGIN alone holds nothing). A refused open disposes
''' the connection, rolling the read back; nothing is created.
''' </summary>
Public Module MapAccess

    Private Const BusySeconds As Integer = 3

    ''' <summary>
    ''' Opens the configured map for one call.
    ''' </summary>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <returns>The open map with its read transaction begun; the caller ends it after serialisation and disposes it.</returns>
    ''' <exception cref="BridgeRefusalException">MapAbsent, Busy, Unopenable, NotAMap, VersionBelow or VersionAbove.</exception>
    Public Function OpenRead(config As BridgeConfig) As MapDatabase
        If Not File.Exists(config.MapPath) Then
            Throw Refuse(BridgeRefusalKind.MapAbsent, Facts("mapPath", config.MapPath, "configPath", config.SourcePath))
        End If
        Dim db As MapDatabase
        Try
            db = MapDatabase.OpenReadOnly(config.MapPath)
        Catch ex As SqliteException
            Throw Translate(ex, config)
        End Try
        Try
            Try
                db.BeginRead()
            Catch ex As SqliteException
                Throw Translate(ex, config)
            End Try
            Dim found As Integer
            Dim state As SchemaState
            Try
                state = db.InspectSchema(found)
            Catch ex As SqliteException When ex.SqliteErrorCode = 26
                Throw Refuse(BridgeRefusalKind.NotAMap, Facts("mapPath", config.MapPath))
            Catch ex As SqliteException
                Throw Translate(ex, config)
            End Try
            If state = SchemaState.Fresh OrElse state = SchemaState.Foreign Then
                Throw Refuse(BridgeRefusalKind.NotAMap, Facts("mapPath", config.MapPath))
            End If
            If found < BridgeSchemaPin.Required Then
                Throw Refuse(BridgeRefusalKind.VersionBelow, Facts("mapPath", config.MapPath, "found", found.ToString(), "required", BridgeSchemaPin.Required.ToString()))
            End If
            If found > BridgeSchemaPin.Required Then
                Throw Refuse(BridgeRefusalKind.VersionAbove, Facts("mapPath", config.MapPath, "found", found.ToString(), "required", BridgeSchemaPin.Required.ToString()))
            End If
            Return db
        Catch
            db.Dispose()
            Throw
        End Try
    End Function

    ''' <summary>
    ''' Names a driver failure on the map: SQLITE_BUSY becomes Busy, anything else Unopenable with the driver's text.
    ''' </summary>
    ''' <param name="ex">The driver exception.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <returns>The refusal to throw.</returns>
    Public Function Translate(ex As SqliteException, config As BridgeConfig) As BridgeRefusalException
        If ex.SqliteErrorCode = 5 Then
            Return Refuse(BridgeRefusalKind.Busy, Facts("mapPath", config.MapPath, "seconds", BusySeconds.ToString()))
        End If
        Return Refuse(BridgeRefusalKind.Unopenable, Facts("role", "map", "path", config.MapPath, "driver", ex.Message))
    End Function

    Private Function Refuse(kind As BridgeRefusalKind, facts As IDictionary(Of String, String)) As BridgeRefusalException
        Return New BridgeRefusalException(BridgeRefusal.Named(kind, facts))
    End Function

    Private Function Facts(ParamArray pairs As String()) As IDictionary(Of String, String)
        Dim result As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(pairs(i)) = pairs(i + 1)
        Next
        Return result
    End Function

End Module

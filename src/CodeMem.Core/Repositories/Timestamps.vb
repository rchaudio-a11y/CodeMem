' File: Timestamps.vb
' Project: CodeMem.Core
' Description: The one ISO-8601 UTC timestamp format written to the map.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.Globalization

''' <summary>
''' Formats timestamps for created_utc, started_utc and finished_utc.
''' </summary>
Public Module Timestamps

    ''' <summary>
    ''' The current UTC time as ISO-8601 with seven fractional digits and a Z suffix.
    ''' </summary>
    ''' <returns>The timestamp text.</returns>
    Public Function NowUtc() As String
        Return DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture)
    End Function

End Module

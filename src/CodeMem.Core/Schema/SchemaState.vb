' File: SchemaState.vb
' Project: CodeMem.Core
' Description: What MapDatabase.InspectSchema found at --db under the write lock (fixpack 002 data-model.md "Map states at open").
' Author: RCH Automation LLC
' Created: 2026-09-10
'
' 2026-09-17 (feature 005, T009): Version2 added - a 004-era map migrates to 3 in place (research R63).

''' <summary>
''' The six states a file at <c>--db</c> can be in when the extractor holds the write lock. Detection is one method,
''' <c>MapDatabase.InspectSchema</c> (Article XII); the action for each state is taken by <c>ExtractionRun</c>.
''' </summary>
Public Enum SchemaState
    ''' <summary>The file is absent, 0 bytes, or a SQLite file whose sqlite_master has no user table (FR-105): create version 1, insert the identity row, migrate to 2, then to 3.</summary>
    Fresh
    ''' <summary>map_identity.schema_version = 1: migrate to 2, then to 3, in place (FR-112).</summary>
    Version1
    ''' <summary>map_identity.schema_version = 2: migrate to 3 in place (feature 005, FR-429).</summary>
    Version2
    ''' <summary>map_identity.schema_version = SchemaVersion.Current: proceed.</summary>
    Current
    ''' <summary>User tables exist but there is no map_identity table, or it has no row: not a map, refused untouched (FR-105, Article IX).</summary>
    Foreign
    ''' <summary>map_identity.schema_version is greater than SchemaVersion.Current (or below 1): refused with a schema-version mismatch (FR-114).</summary>
    Newer
End Enum

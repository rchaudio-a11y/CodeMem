' File: StoreAccess.vb
' Project: CodeMem.Bridging
' Description: The one door for the registry: open the store read-only, read code_map_solutions, close; RegistryAbsent from the query's own answer (FR-304, INC1).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core
Imports Microsoft.Data.Sqlite

''' <summary>
''' Every registry read goes through <see cref="ReadRegistry"/>; the store is never held across anything and no other table is named.
''' </summary>
Public Module StoreAccess

    ''' <summary>
    ''' Reads every registry row, ordered by key.
    ''' </summary>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <returns>The rows.</returns>
    ''' <exception cref="BridgeRefusalException">Unopenable naming the store path, or RegistryAbsent.</exception>
    Public Function ReadRegistry(config As BridgeConfig) As List(Of RegistryRecord)
        Dim store As StoreDatabase
        Try
            store = StoreDatabase.OpenReadOnly(config.StorePath)
        Catch ex As SqliteException
            Throw Unopenable(config, ex.Message)
        End Try
        Using store
            Try
                Return CodeMapSolutionsRepository.ReadAll(store)
            Catch ex As RegistryTableMissingException
                Throw New BridgeRefusalException(BridgeRefusal.Named(BridgeRefusalKind.RegistryAbsent, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"storePath", config.StorePath}}))
            Catch ex As SqliteException
                Throw Unopenable(config, ex.Message)
            End Try
        End Using
    End Function

    Private Function Unopenable(config As BridgeConfig, driver As String) As BridgeRefusalException
        Return New BridgeRefusalException(BridgeRefusal.Named(BridgeRefusalKind.Unopenable, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"role", "store"}, {"path", config.StorePath}, {"driver", driver}}))
    End Function

End Module

' File: SymbolResolver.vb
' Project: CodeMem.Bridging
' Description: The symbol-level refusals the id-taking tools share, in 058 §6's order: not found, out of scope, retired; and the project-row rule (FR-311, FR-314, 060 §3.1).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' One helper for symbol_detail, references, type_usages and the projectSymbolId filter: a mapped id only, active only, in scope only.
''' </summary>
Public Module SymbolResolver

    ''' <summary>
    ''' The argument stage: a missing symbolId is refused before the configuration is read.
    ''' </summary>
    ''' <param name="symbolId">The id, or Nothing.</param>
    ''' <exception cref="BridgeRefusalException">SymbolIdMissing.</exception>
    Public Sub RequireSymbolId(symbolId As Long?)
        If Not symbolId.HasValue Then Throw New BridgeRefusalException(BridgeRefusal.Named(BridgeRefusalKind.SymbolIdMissing, Nothing))
    End Sub

    ''' <summary>
    ''' Reads the row and refuses SymbolNotFound, then SymbolOutOfScope, then SymbolRetired.
    ''' </summary>
    ''' <param name="map">The call's open map.</param>
    ''' <param name="scope">The resolved scope.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="symbolId">The id.</param>
    ''' <returns>The active row in scope.</returns>
    Public Function RequireActive(map As MapDatabase, scope As ResolvedScope, config As BridgeConfig, symbolId As Long) As SymbolRecord
        Dim row As SymbolRecord = CodeSymbolsRepository.ReadById(map, symbolId)
        Dim id As String = symbolId.ToString(Globalization.CultureInfo.InvariantCulture)
        If row Is Nothing Then
            Throw Refuse(BridgeRefusalKind.SymbolNotFound, "mapPath", config.MapPath, "symbolId", id)
        End If
        If Not scope.Contains(row.SolutionId) Then
            Throw Refuse(BridgeRefusalKind.SymbolOutOfScope, "id", id, "name", row.Name, "solutionKey", row.SolutionKey, "scope", scope.Describe())
        End If
        If Not row.IsActive Then
            Throw Refuse(BridgeRefusalKind.SymbolRetired, "id", id, "name", row.Name, "kind", row.Kind, "path", row.Path, "solutionKey", row.SolutionKey, "lastSeenRunId", row.LastSeenRunId.ToString(Globalization.CultureInfo.InvariantCulture))
        End If
        Return row
    End Function

    ''' <summary>
    ''' A projectSymbolId: the active row in scope, and it must be a project row.
    ''' </summary>
    ''' <param name="map">The call's open map.</param>
    ''' <param name="scope">The resolved scope.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="projectSymbolId">The id.</param>
    ''' <returns>The project row.</returns>
    Public Function RequireProjectRow(map As MapDatabase, scope As ResolvedScope, config As BridgeConfig, projectSymbolId As Long) As SymbolRecord
        Dim row As SymbolRecord = RequireActive(map, scope, config, projectSymbolId)
        If Not String.Equals(row.Kind, "project", StringComparison.Ordinal) Then
            Throw Refuse(BridgeRefusalKind.NotAProjectRow, "id", row.Id.ToString(Globalization.CultureInfo.InvariantCulture), "name", row.Name, "kind", row.Kind, "path", row.Path, "line", row.StartLine.ToString(Globalization.CultureInfo.InvariantCulture))
        End If
        Return row
    End Function

    Private Function Refuse(kind As BridgeRefusalKind, ParamArray pairs As String()) As BridgeRefusalException
        Dim facts As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            facts(pairs(i)) = pairs(i + 1)
        Next
        Return New BridgeRefusalException(BridgeRefusal.Named(kind, facts))
    End Function

End Module

' File: SymbolSearchReader.vb
' Project: CodeMem.Bridging
' Description: symbol_search: filters validated at the argument stage, raw rows folded into declarations, the project filter narrowing rows and never the total, the 200 cap (FR-313, FR-340; 058 §3.2, research R50).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' The reader behind BridgeTools.SymbolSearch.
''' </summary>
Public Module SymbolSearchReader

    ''' <summary>The cap on presented declarations.</summary>
    Public Const Cap As Integer = 200

    ''' <summary>
    ''' The argument stage: at least one filter, and a known kind.
    ''' </summary>
    ''' <param name="name">The name filter, or Nothing.</param>
    ''' <param name="kind">The kind filter, or Nothing.</param>
    ''' <exception cref="BridgeRefusalException">FilterMissing or KindUnknown.</exception>
    Public Sub ValidateFilters(name As String, kind As String)
        If name Is Nothing AndAlso kind Is Nothing Then Throw New BridgeRefusalException(BridgeRefusal.Named(BridgeRefusalKind.FilterMissing, Nothing))
        If kind IsNot Nothing AndAlso Not SymbolKinds.IsKnown(kind) Then
            Throw New BridgeRefusalException(BridgeRefusal.Named(BridgeRefusalKind.KindUnknown, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"given", kind}, {"kinds", String.Join(", ", SymbolKinds.All)}}))
        End If
    End Sub

    ''' <summary>
    ''' Builds the envelope.
    ''' </summary>
    ''' <param name="map">The call's open map.</param>
    ''' <param name="scope">The resolved scope.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="name">The name filter, or Nothing.</param>
    ''' <param name="kind">The kind filter, or Nothing.</param>
    ''' <param name="projectSymbolId">The project row to narrow to, or Nothing.</param>
    ''' <returns>The envelope.</returns>
    Public Function Read(map As MapDatabase, scope As ResolvedScope, config As BridgeConfig, name As String, kind As String, projectSymbolId As Long?) As SymbolSearchEnvelope
        If projectSymbolId.HasValue Then SymbolResolver.RequireProjectRow(map, scope, config, projectSymbolId.Value)
        Dim declarations As List(Of DeclarationEnvelope) = TwinFolder.Fold(CodeSymbolsRepository.Search(map, scope.SolutionIds, name, kind))
        Dim total As Integer = declarations.Count
        If projectSymbolId.HasValue Then
            declarations = declarations.FindAll(Function(d As DeclarationEnvelope) d.Project IsNot Nothing AndAlso d.Project.Id = projectSymbolId.Value)
        End If
        Dim truncated As Boolean = declarations.Count > Cap
        If truncated Then declarations = declarations.GetRange(0, Cap)
        Return New SymbolSearchEnvelope With {
            .ReadAtUtc = BridgeJson.NowUtc(),
            .Scope = scope.Envelope,
            .Filters = New SearchFiltersEnvelope With {.Name = name, .Kind = kind, .ProjectSymbolId = projectSymbolId},
            .Total = total,
            .Truncated = truncated,
            .Symbols = declarations}
    End Function

End Module

' File: SymbolDetailReader.vb
' Project: CodeMem.Bridging
' Description: symbol_detail: the active symbol in scope, its parts with the declaring one flagged, outbound and inbound edges under all eight verbs, the twin header (FR-309, FR-340; 058 §3.3).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' The reader behind BridgeTools.SymbolDetail. External targets carry their doc-comment id and the external marking.
''' </summary>
Public Module SymbolDetailReader

    ''' <summary>
    ''' Builds the envelope.
    ''' </summary>
    ''' <param name="map">The call's open map.</param>
    ''' <param name="scope">The resolved scope.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="symbolId">The mapped symbol id.</param>
    ''' <returns>The envelope.</returns>
    Public Function Read(map As MapDatabase, scope As ResolvedScope, config As BridgeConfig, symbolId As Long) As SymbolDetailEnvelope
        Dim row As SymbolRecord = SymbolResolver.RequireActive(map, scope, config, symbolId)
        Dim parts As List(Of PartEnvelope) = New List(Of PartEnvelope)()
        For Each part As PartRecord In CodePartsRepository.ReadParts(map, row.Id)
            Dim holdsName As Boolean = String.Equals(part.Path, row.Path, StringComparison.Ordinal) AndAlso row.StartOffset >= part.StartOffset AndAlso row.StartOffset < part.StartOffset + part.Length
            parts.Add(New PartEnvelope With {.Path = part.Path, .Line = part.StartLine, .Column = part.StartColumn, .ContainsDeclaringLocation = holdsName})
        Next
        Dim outbound As Dictionary(Of String, List(Of OutboundEdgeEnvelope)) = New Dictionary(Of String, List(Of OutboundEdgeEnvelope))(StringComparer.Ordinal)
        Dim inbound As Dictionary(Of String, List(Of InboundEdgeEnvelope)) = New Dictionary(Of String, List(Of InboundEdgeEnvelope))(StringComparer.Ordinal)
        For Each verb As String In EdgeVerbs.All
            outbound(verb) = New List(Of OutboundEdgeEnvelope)()
            inbound(verb) = New List(Of InboundEdgeEnvelope)()
        Next
        For Each edge As EdgeRecord In CodeEdgesRepository.ReadOutbound(map, row.Id)
            outbound(edge.Verb).Add(New OutboundEdgeEnvelope With {
                .TargetSymbolId = edge.TargetSymbolId,
                .TargetDocCommentId = edge.TargetDocCommentId,
                .TargetName = edge.TargetName,
                .External = Not edge.TargetSymbolId.HasValue,
                .Via = TwinFolder.NamedRef(edge.ViaSymbolId, edge.ViaName),
                .Path = edge.Path,
                .Line = edge.StartLine,
                .Column = edge.StartColumn})
        Next
        For Each edge As EdgeRecord In CodeEdgesRepository.ReadInbound(map, row.Id)
            inbound(edge.Verb).Add(New InboundEdgeEnvelope With {
                .SourceSymbolId = edge.SourceSymbolId,
                .SourceName = edge.SourceName,
                .SourceKind = edge.SourceKind,
                .Path = edge.Path,
                .Line = edge.StartLine,
                .Column = edge.StartColumn})
        Next
        Return New SymbolDetailEnvelope With {
            .ReadAtUtc = BridgeJson.NowUtc(),
            .Scope = scope.Envelope,
            .Symbol = New DetailSymbolEnvelope With {
                .SolutionId = row.SolutionId,
                .SolutionKey = row.SolutionKey,
                .Id = row.Id,
                .DocCommentId = row.DocCommentId,
                .Kind = row.Kind,
                .Name = row.Name,
                .Container = TwinFolder.NamedRef(row.ContainerId, row.ContainerName),
                .Path = row.Path,
                .Line = row.StartLine,
                .Column = row.StartColumn,
                .CompiledInto = TwinFolder.HeaderOf(row, CodeSymbolsRepository.ReadTwins(map, row.Id))},
            .Parts = parts,
            .Outbound = outbound,
            .Inbound = inbound}
    End Function

End Module

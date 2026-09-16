' File: TypeUsagesReader.vb
' Project: CodeMem.Bridging
' Description: type_usages: a type kind only, the union the finding names deduplicated by edge id, grouped counts, fromInside and fromOutside (FR-316..FR-319, spec Q3; contracts/tools.md §3.6).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' The reader behind BridgeTools.TypeUsages. The query lives in CodeEdgesRepository.ReadTypeUsages; this module checks the kind and shapes
''' the result: byVerb over the seven non-part_of verbs with zeros, total, fromOutside, then the flat list.
''' </summary>
Public Module TypeUsagesReader

    ''' <summary>
    ''' Builds the envelope.
    ''' </summary>
    ''' <param name="map">The call's open map.</param>
    ''' <param name="scope">The resolved scope.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="symbolId">The mapped symbol id of a type.</param>
    ''' <returns>The envelope.</returns>
    Public Function Read(map As MapDatabase, scope As ResolvedScope, config As BridgeConfig, symbolId As Long) As TypeUsagesEnvelope
        Dim row As SymbolRecord = SymbolResolver.RequireActive(map, scope, config, symbolId)
        If Array.IndexOf(SymbolKinds.TypeKinds, row.Kind) < 0 Then
            Throw New BridgeRefusalException(BridgeRefusal.Named(BridgeRefusalKind.NotAType, New Dictionary(Of String, String)(StringComparer.Ordinal) From {
                {"id", row.Id.ToString(Globalization.CultureInfo.InvariantCulture)}, {"name", row.Name}, {"kind", row.Kind}}))
        End If
        Dim byVerb As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)(StringComparer.Ordinal)
        For Each verb As String In EdgeVerbs.NonPartOf
            byVerb(verb) = 0
        Next
        Dim occurrences As List(Of TypeUsageOccurrenceEnvelope) = New List(Of TypeUsageOccurrenceEnvelope)()
        Dim fromOutside As Integer = 0
        For Each edge As EdgeRecord In CodeEdgesRepository.ReadTypeUsages(map, row.Id)
            byVerb(edge.Verb) += 1
            If Not edge.FromInside Then fromOutside += 1
            occurrences.Add(New TypeUsageOccurrenceEnvelope With {
                .Verb = edge.Verb,
                .Path = edge.Path,
                .Line = edge.StartLine,
                .Column = edge.StartColumn,
                .Source = New SourceRefEnvelope With {.Id = edge.SourceSymbolId, .Name = edge.SourceName, .Kind = edge.SourceKind},
                .Target = New TypeUsageTargetEnvelope With {
                    .Id = edge.TargetSymbolId.Value,
                    .DocCommentId = edge.TargetDocCommentId,
                    .Name = edge.TargetName,
                    .Kind = edge.TargetKind,
                    .Role = edge.TargetRole,
                    .External = False},
                .FromInside = edge.FromInside,
                .EdgeId = edge.Id})
        Next
        Return New TypeUsagesEnvelope With {
            .ReadAtUtc = BridgeJson.NowUtc(),
            .Scope = scope.Envelope,
            .Symbol = New TypeSymbolEnvelope With {.SolutionId = row.SolutionId, .SolutionKey = row.SolutionKey, .Id = row.Id, .DocCommentId = row.DocCommentId, .Kind = row.Kind, .Name = row.Name, .Path = row.Path, .Line = row.StartLine},
            .ByVerb = byVerb,
            .Total = occurrences.Count,
            .FromOutside = fromOutside,
            .Occurrences = occurrences}
    End Function

End Module

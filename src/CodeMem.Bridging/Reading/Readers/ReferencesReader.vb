' File: ReferencesReader.vb
' Project: CodeMem.Bridging
' Description: references: every occurrence whose compiler-resolved target is the active symbol in scope, containment excluded, uncapped (FR-311, FR-318; 058 §3.4).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' The reader behind BridgeTools.References.
''' </summary>
Public Module ReferencesReader

    ''' <summary>
    ''' Builds the envelope.
    ''' </summary>
    ''' <param name="map">The call's open map.</param>
    ''' <param name="scope">The resolved scope.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="symbolId">The mapped symbol id.</param>
    ''' <returns>The envelope.</returns>
    Public Function Read(map As MapDatabase, scope As ResolvedScope, config As BridgeConfig, symbolId As Long) As ReferencesEnvelope
        Dim row As SymbolRecord = SymbolResolver.RequireActive(map, scope, config, symbolId)
        Dim occurrences As List(Of OccurrenceEnvelope) = New List(Of OccurrenceEnvelope)()
        For Each edge As EdgeRecord In CodeEdgesRepository.ReadReferences(map, row.Id)
            occurrences.Add(New OccurrenceEnvelope With {
                .Verb = edge.Verb,
                .Path = edge.Path,
                .Line = edge.StartLine,
                .Column = edge.StartColumn,
                .Source = New SourceRefEnvelope With {.Id = edge.SourceSymbolId, .Name = edge.SourceName, .Kind = edge.SourceKind}})
        Next
        Return New ReferencesEnvelope With {
            .ReadAtUtc = BridgeJson.NowUtc(),
            .Scope = scope.Envelope,
            .Symbol = New ReferenceSymbolEnvelope With {.SolutionId = row.SolutionId, .SolutionKey = row.SolutionKey, .Id = row.Id, .DocCommentId = row.DocCommentId, .Kind = row.Kind, .Name = row.Name},
            .Count = occurrences.Count,
            .Occurrences = occurrences}
    End Function

End Module

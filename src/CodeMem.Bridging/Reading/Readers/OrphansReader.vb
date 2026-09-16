' File: OrphansReader.vb
' Project: CodeMem.Bridging
' Description: orphans: the argument stage for kind, the project-row rule, the 060 shape with every active project in scope and the examined kinds (FR-313; 059 §3.1 as amended by 060 §3.1).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' The reader behind BridgeTools.Orphans. The rule lives in CodeSymbolsRepository.ReadOrphans; this module shapes the result.
''' </summary>
Public Module OrphansReader

    ''' <summary>
    ''' The argument stage: a known kind, and one orphans examines.
    ''' </summary>
    ''' <param name="kind">The kind filter, or Nothing.</param>
    ''' <exception cref="BridgeRefusalException">KindUnknown or KindNotExamined.</exception>
    Public Sub ValidateArguments(kind As String)
        If kind Is Nothing Then Return
        If Not SymbolKinds.IsKnown(kind) Then
            Throw New BridgeRefusalException(BridgeRefusal.Named(BridgeRefusalKind.KindUnknown, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"given", kind}, {"kinds", String.Join(", ", SymbolKinds.All)}}))
        End If
        If Array.IndexOf(SymbolKinds.NotExamined, kind) >= 0 Then
            Dim reason As String = If(kind = "project", "project rows are roots, unreferenced by construction", "the map holds no namespace rows to examine")
            Throw New BridgeRefusalException(BridgeRefusal.Named(BridgeRefusalKind.KindNotExamined, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"given", kind}, {"reason", reason}}))
        End If
    End Sub

    ''' <summary>
    ''' Builds the envelope.
    ''' </summary>
    ''' <param name="map">The call's open map.</param>
    ''' <param name="scope">The resolved scope.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="kind">The kind filter, or Nothing.</param>
    ''' <param name="projectSymbolId">The project row to narrow to, or Nothing.</param>
    ''' <returns>The envelope.</returns>
    Public Function Read(map As MapDatabase, scope As ResolvedScope, config As BridgeConfig, kind As String, projectSymbolId As Long?) As OrphansEnvelope
        If projectSymbolId.HasValue Then SymbolResolver.RequireProjectRow(map, scope, config, projectSymbolId.Value)
        Dim rows As List(Of OrphanRecord) = CodeSymbolsRepository.ReadOrphans(map, scope.SolutionIds, kind, projectSymbolId)
        Dim byProject As List(Of ProjectCountEnvelope) = New List(Of ProjectCountEnvelope)()
        For Each project As SymbolRecord In CodeSymbolsRepository.ReadProjects(map, scope.SolutionIds)
            Dim projectId As Long = project.Id
            byProject.Add(New ProjectCountEnvelope With {
                .SolutionId = project.SolutionId,
                .ProjectSymbolId = projectId,
                .Name = project.Name,
                .Count = rows.FindAll(Function(r As OrphanRecord) r.ProjectSymbolId.HasValue AndAlso r.ProjectSymbolId.Value = projectId).Count})
        Next
        Dim byKind As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)(StringComparer.Ordinal)
        For Each examined As String In If(kind Is Nothing, SymbolKinds.Examined, New String() {kind})
            Dim k As String = examined
            byKind(k) = rows.FindAll(Function(r As OrphanRecord) r.Kind = k).Count
        Next
        Dim orphans As List(Of OrphanRowEnvelope) = New List(Of OrphanRowEnvelope)()
        For Each row As OrphanRecord In rows
            orphans.Add(New OrphanRowEnvelope With {
                .SolutionId = row.SolutionId,
                .SolutionKey = row.SolutionKey,
                .Id = row.Id,
                .DocCommentId = row.DocCommentId,
                .Kind = row.Kind,
                .Name = row.Name,
                .Container = TwinFolder.NamedRef(row.ContainerId, row.ContainerName),
                .Path = row.Path,
                .Line = row.StartLine,
                .Project = TwinFolder.NamedRef(row.ProjectSymbolId, row.ProjectName)})
        Next
        Return New OrphansEnvelope With {
            .ReadAtUtc = BridgeJson.NowUtc(),
            .Scope = scope.Envelope,
            .Filters = New OrphanFiltersEnvelope With {.Kind = kind, .ProjectSymbolId = projectSymbolId},
            .NotExamined = New List(Of String)(SymbolKinds.NotExamined),
            .Total = orphans.Count,
            .ByProject = byProject,
            .ByKind = byKind,
            .Orphans = orphans}
    End Function

End Module

' File: RenameCandidatesReader.vb
' Project: CodeMem.Bridging
' Description: rename_candidates (feature 006, T012; FR-501-FR-507; research R76-R78): the run and symbol checks, the candidates, the runs of spec Q5, the sides with their projects, the count check, the envelope.
' Author: RCH Automation LLC
' Created: 2026-09-29
'
' No SQL here: every read is a named Core method. The retiring run is derived in one place, ExtractRunsRepository.ReadRetiringRun (spec
' Q2). Candidates are presented as rows, never folded (spec Q1). The reader resolves nothing and writes nothing.

Imports CodeMem.Core

''' <summary>
''' Reads one solution's rename evidence, optionally narrowed to one run and to one retired symbol.
''' </summary>
Public Module RenameCandidatesReader

    Private Const Completed As String = "completed"
    Private Const FailedRun As String = "failed run"
    Private Const Filtered As String = "filtered by retiredSymbolId"

    ''' <summary>The empty answer under retiredSymbolId (data-model §2).</summary>
    Public Const RetiredSentence As String = "No rename candidate names this symbol in the runs examined. A changed signature or body leaves no candidate by design (the body hash differs): search for the current symbol by name."

    ''' <summary>The empty answer without retiredSymbolId (data-model §2).</summary>
    Public Const UnfilteredSentence As String = "No rename candidate was recorded in the runs examined. A changed signature or body leaves no candidate by design (the body hash differs)."

    ''' <summary>
    ''' Builds the answer, or refuses RunNotFound, RunOutOfScope, SymbolNotFound, SymbolOutOfScope or CandidateCountMismatch by name.
    ''' </summary>
    ''' <param name="map">The call's open map.</param>
    ''' <param name="scope">The resolved scope (one solution).</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="runId">One run, or Nothing.</param>
    ''' <param name="retiredSymbolId">One retired symbol, or Nothing.</param>
    ''' <returns>The envelope.</returns>
    Public Function Read(map As MapDatabase, scope As ResolvedScope, config As BridgeConfig, runId As Long?, retiredSymbolId As Long?) As RenameCandidatesEnvelope
        Dim solutionId As Long = scope.SolutionIds(0)
        Dim solutionKey As String = scope.Envelope.SolutionKey

        Dim askedRun As RunRecord = Nothing
        If runId.HasValue Then
            askedRun = ExtractRunsRepository.ReadById(map, runId.Value)
            If askedRun Is Nothing Then Throw Refuse(BridgeRefusalKind.RunNotFound, "mapPath", config.MapPath, "runId", Invariant(runId.Value))
            If askedRun.SolutionId <> solutionId Then
                Dim owner As SolutionRecord = SolutionsRepository.ReadById(map, askedRun.SolutionId)
                Throw Refuse(BridgeRefusalKind.RunOutOfScope, "runId", Invariant(runId.Value), "runKey", If(owner Is Nothing, "?", owner.Key), "solutionKey", solutionKey)
            End If
        End If

        Dim named As SymbolRecord = Nothing
        If retiredSymbolId.HasValue Then named = SymbolResolver.RequireInScope(map, scope, config, retiredSymbolId.Value)

        Dim rows As List(Of RenameCandidateRecord) = RenameCandidatesRepository.ReadCandidates(map, solutionId, runId, retiredSymbolId)

        Dim retiringRun As RunRecord = Nothing
        If named IsNot Nothing AndAlso Not named.IsActive Then retiringRun = ExtractRunsRepository.ReadRetiringRun(map, solutionId, named.LastSeenRunId)

        Dim runs As List(Of RunRecord)
        If askedRun IsNot Nothing Then
            runs = New List(Of RunRecord) From {askedRun}
        ElseIf named IsNot Nothing Then
            runs = RunsNaming(map, rows, retiringRun)
        Else
            runs = ExtractRunsRepository.ReadBySolution(map, solutionId)
        End If

        Dim symbols As Dictionary(Of Long, SymbolRecord) = New Dictionary(Of Long, SymbolRecord)()
        Dim candidates As List(Of CandidateEnvelope) = New List(Of CandidateEnvelope)()
        Dim returnedByRun As Dictionary(Of Long, Integer) = New Dictionary(Of Long, Integer)()
        For Each row As RenameCandidateRecord In rows
            candidates.Add(New CandidateEnvelope With {
                .RunId = row.RunId,
                .Retired = SideOf(Lookup(map, symbols, row.RetiredSymbolId)),
                .[New] = SideOf(Lookup(map, symbols, row.NewSymbolId)),
                .SamePath = row.SamePath,
                .OffsetDistance = row.OffsetDistance,
                .Rank = row.Rank})
            Dim count As Integer = 0
            returnedByRun.TryGetValue(row.RunId, count)
            returnedByRun(row.RunId) = count + 1
        Next

        Dim examined As List(Of ExaminedRunEnvelope) = New List(Of ExaminedRunEnvelope)()
        For Each run As RunRecord In runs
            Dim returned As Integer = 0
            returnedByRun.TryGetValue(run.Id, returned)
            Dim entry As ExaminedRunEnvelope = New ExaminedRunEnvelope With {
                .RunId = run.Id,
                .Outcome = run.Outcome,
                .FinishedUtc = run.FinishedUtc,
                .SymbolsRetired = run.Counts.SymbolsRetired,
                .RenameCandidatesRecorded = run.Counts.RenameCandidates,
                .CandidatesReturned = returned}
            If named IsNot Nothing Then
                entry.CountNotCheckedReason = Filtered
            ElseIf Not String.Equals(run.Outcome, Completed, StringComparison.Ordinal) Then
                entry.CountNotCheckedReason = FailedRun
            ElseIf returned <> run.Counts.RenameCandidates Then
                Throw Refuse(BridgeRefusalKind.CandidateCountMismatch, "runId", Invariant(run.Id), "solutionKey", solutionKey, "recorded", Invariant(run.Counts.RenameCandidates), "found", Invariant(returned))
            Else
                entry.CountChecked = True
            End If
            examined.Add(entry)
        Next

        Dim symbolBlock As CandidateSideEnvelope = Nothing
        If named IsNot Nothing Then
            symbolBlock = SideOf(named)
            symbolBlock.LastSeenRunId = named.LastSeenRunId
            If retiringRun IsNot Nothing Then symbolBlock.RetiredInRunId = retiringRun.Id
        End If

        Return New RenameCandidatesEnvelope With {
            .ReadAtUtc = BridgeJson.NowUtc(),
            .Scope = scope.Envelope,
            .Filters = New RenameCandidatesFiltersEnvelope With {.RunId = runId, .RetiredSymbolId = retiredSymbolId},
            .Symbol = symbolBlock,
            .Runs = examined,
            .Total = candidates.Count,
            .Candidates = candidates,
            .Note = If(candidates.Count > 0, Nothing, If(named IsNot Nothing, RetiredSentence, UnfilteredSentence))}
    End Function

    ''' <summary>
    ''' Under retiredSymbolId alone: the runs that wrote a candidate naming it, plus the run that retired it when it is retired now; no
    ''' duplicates, newest first (spec Q2) - never every run of the solution.
    ''' </summary>
    Private Function RunsNaming(map As MapDatabase, rows As List(Of RenameCandidateRecord), retiringRun As RunRecord) As List(Of RunRecord)
        Dim ids As SortedSet(Of Long) = New SortedSet(Of Long)()
        For Each row As RenameCandidateRecord In rows
            ids.Add(row.RunId)
        Next
        If retiringRun IsNot Nothing Then ids.Add(retiringRun.Id)
        Dim runs As List(Of RunRecord) = New List(Of RunRecord)()
        For Each id As Long In ids.Reverse()
            If retiringRun IsNot Nothing AndAlso retiringRun.Id = id Then
                runs.Add(retiringRun)
            Else
                runs.Add(ExtractRunsRepository.ReadById(map, id))
            End If
        Next
        Return runs
    End Function

    Private Function Lookup(map As MapDatabase, symbols As Dictionary(Of Long, SymbolRecord), id As Long) As SymbolRecord
        Dim row As SymbolRecord = Nothing
        If symbols.TryGetValue(id, row) Then Return row
        row = CodeSymbolsRepository.ReadById(map, id)
        If row Is Nothing Then Throw New InvalidOperationException("a rename candidate names symbol " & Invariant(id) & ", which the map does not hold")
        symbols(id) = row
        Return row
    End Function

    Private Function SideOf(row As SymbolRecord) As CandidateSideEnvelope
        Return New CandidateSideEnvelope With {
            .Id = row.Id,
            .DocCommentId = row.DocCommentId,
            .Kind = row.Kind,
            .Name = row.Name,
            .Container = TwinFolder.NamedRef(row.ContainerId, row.ContainerName),
            .Project = TwinFolder.NamedRef(row.ProjectSymbolId, row.ProjectName),
            .Path = row.Path,
            .Line = row.StartLine,
            .IsActive = row.IsActive}
    End Function

    Private Function Invariant(value As Long) As String
        Return value.ToString(Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Function Refuse(kind As BridgeRefusalKind, ParamArray pairs As String()) As BridgeRefusalException
        Dim facts As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            facts(pairs(i)) = pairs(i + 1)
        Next
        Return New BridgeRefusalException(BridgeRefusal.Named(kind, facts))
    End Function

End Module

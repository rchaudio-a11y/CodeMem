' File: SolutionsReader.vb
' Project: CodeMem.Bridging
' Description: The solutions envelope, 056 §1.1: the map identity and every solution with its latest run of any outcome (FR-309, INC4).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' Reads on the call's open map; nothing is derived beyond dirtyState's three names.
''' </summary>
Public Module SolutionsReader

    ''' <summary>
    ''' Builds the envelope.
    ''' </summary>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="map">The call's open map.</param>
    ''' <returns>The envelope.</returns>
    Public Function Read(config As BridgeConfig, map As MapDatabase) As SolutionsEnvelope
        Dim identity As MapIdentityRecord = MapIdentityRepository.ReadIdentity(map)
        Dim envelope As SolutionsEnvelope = New SolutionsEnvelope With {
            .Map = New MapIdentityEnvelope With {.MapGuid = identity.MapGuid, .SchemaVersion = identity.SchemaVersion, .CreatedUtc = identity.CreatedUtc},
            .MapPath = config.MapPath,
            .ReadAtUtc = BridgeJson.NowUtc(),
            .Solutions = New List(Of SolutionEnvelope)()}
        For Each solution As SolutionRecord In SolutionsRepository.ReadAll(map)
            Dim run As RunRecord = ExtractRunsRepository.ReadLatest(map, solution.Id)
            envelope.Solutions.Add(New SolutionEnvelope With {
                .Key = solution.Key,
                .Name = solution.Name,
                .RepoRoot = solution.RepoRoot,
                .LastSeenPath = solution.LastSeenPath,
                .LatestRun = If(run Is Nothing, Nothing, LatestRunOf(run))})
        Next
        Return envelope
    End Function

    ''' <summary>
    ''' The run as 056 presents it.
    ''' </summary>
    ''' <param name="run">The run row.</param>
    ''' <returns>The envelope.</returns>
    Public Function LatestRunOf(run As RunRecord) As LatestRunEnvelope
        Dim dirtyState As String = "unknown"
        If run.CommitSha IsNot Nothing Then dirtyState = If(run.IsDirty.HasValue AndAlso run.IsDirty.Value, "dirty", "clean")
        Return New LatestRunEnvelope With {
            .RunId = run.Id,
            .Outcome = run.Outcome,
            .SourceDigest = run.SourceDigest,
            .CommitSha = run.CommitSha,
            .DirtyState = dirtyState,
            .SchemaVersion = run.SchemaVersion,
            .SdkVersion = run.SdkVersion,
            .StartedUtc = run.StartedUtc,
            .FinishedUtc = run.FinishedUtc,
            .SymbolsObserved = run.Counts.SymbolsObserved,
            .SymbolsMatched = run.Counts.SymbolsMatched,
            .SymbolsReactivated = run.Counts.SymbolsReactivated,
            .SymbolsNew = run.Counts.SymbolsNew,
            .SymbolsRetired = run.Counts.SymbolsRetired,
            .RegistryActiveBefore = run.Counts.RegistryActiveBefore,
            .NotesOrphaned = run.Counts.NotesOrphaned,
            .RenameCandidates = run.Counts.RenameCandidates,
            .UnaccountedObserved = run.Counts.UnaccountedObserved,
            .UnaccountedRegistry = run.Counts.UnaccountedRegistry}
    End Function

End Module

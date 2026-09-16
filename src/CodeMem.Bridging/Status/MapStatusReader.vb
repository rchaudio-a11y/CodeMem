' File: MapStatusReader.vb
' Project: CodeMem.Bridging
' Description: map_status: one entry per active, bound registry row with every fact and one verdict in the ruled order; unbound and inactive rows listed (FR-320..FR-324, spec Q4; research R52, INC4).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' Verdict order: map_missing_solution, no_git, dirty, diverged, behind, current. The latest completed run is the one compared (INC4).
''' Also the door extract(stale) consults.
''' </summary>
Public Module MapStatusReader

    ''' <summary>
    ''' Builds the envelope on the call's open map from the registry rows read at the store stage.
    ''' </summary>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="registry">The registry rows.</param>
    ''' <param name="map">The call's open map.</param>
    ''' <returns>The envelope.</returns>
    Public Function Read(config As BridgeConfig, registry As List(Of RegistryRecord), map As MapDatabase) As MapStatusEnvelope
        Dim envelope As MapStatusEnvelope = New MapStatusEnvelope With {
            .ReadAtUtc = BridgeJson.NowUtc(),
            .MapPath = config.MapPath,
            .StorePath = config.StorePath,
            .Entries = New List(Of MapStatusEntryEnvelope)(),
            .Unbound = New List(Of UnboundEntryEnvelope)(),
            .Inactive = New List(Of InactiveEntryEnvelope)()}
        Dim bound As List(Of RegistryRecord) = New List(Of RegistryRecord)()
        For Each row As RegistryRecord In registry
            If Not String.Equals(row.State, "active", StringComparison.Ordinal) Then
                envelope.Inactive.Add(New InactiveEntryEnvelope With {.SolutionKey = row.SolutionKey, .ProjectId = row.ProjectId, .State = row.State})
                Continue For
            End If
            If Not row.CodememSolutionId.HasValue Then
                envelope.Unbound.Add(New UnboundEntryEnvelope With {.SolutionKey = row.SolutionKey, .ProjectId = row.ProjectId})
                Continue For
            End If
            bound.Add(row)
        Next
        envelope.Bound = bound.Count
        ' The map facts first, every row's, then the repositories: the repository is never asked while a map read is pending.
        Dim solutions As Dictionary(Of Long, SolutionRecord) = New Dictionary(Of Long, SolutionRecord)()
        Dim runs As Dictionary(Of Long, RunRecord) = New Dictionary(Of Long, RunRecord)()
        For Each row As RegistryRecord In bound
            Dim solution As SolutionRecord = SolutionsRepository.ReadById(map, row.CodememSolutionId.Value)
            solutions(row.Id) = solution
            runs(row.Id) = If(solution Is Nothing, Nothing, ExtractRunsRepository.ReadLatestCompleted(map, solution.Id))
        Next
        For Each row As RegistryRecord In bound
            envelope.Entries.Add(EntryOf(row, solutions(row.Id), runs(row.Id)))
        Next
        Return envelope
    End Function

    ''' <summary>
    ''' One bound row's entry from the map facts already read: the repository's answers, the verdict in the ruled order and its reason.
    ''' </summary>
    ''' <param name="row">An active, bound registry row.</param>
    ''' <param name="solution">The map's solution, or Nothing when the map lacks it.</param>
    ''' <param name="run">The latest completed run, or Nothing.</param>
    ''' <returns>The entry.</returns>
    Public Function EntryOf(row As RegistryRecord, solution As SolutionRecord, run As RunRecord) As MapStatusEntryEnvelope
        Dim entry As MapStatusEntryEnvelope = New MapStatusEntryEnvelope With {
            .SolutionKey = row.SolutionKey,
            .ProjectId = row.ProjectId,
            .CodememSolutionId = row.CodememSolutionId.Value,
            .Head = New HeadEnvelope()}
        Dim id As String = row.CodememSolutionId.Value.ToString(Globalization.CultureInfo.InvariantCulture)
        If solution Is Nothing Then
            entry.Verdict = "map_missing_solution"
            entry.Reason = "registry row '" & row.SolutionKey & "' binds map solution " & id & ", but the map holds no solution " & id & "."
            Return entry
        End If
        entry.RepoRoot = solution.RepoRoot
        If run Is Nothing Then
            entry.Verdict = "no_git"
            entry.Reason = "no completed run: the solution has never been published, so there is nothing to compare."
            Return entry
        End If
        entry.Run = New StatusRunEnvelope With {.RunId = run.Id, .CommitSha = run.CommitSha, .IsDirty = run.IsDirty, .FinishedUtc = run.FinishedUtc}
        If run.CommitSha Is Nothing Then
            entry.Verdict = "no_git"
            entry.Head.Note = "no commit recorded"
            entry.Reason = "run " & run.Id & " recorded no commit: the extractor found no repository with a commit at extraction time."
            Return entry
        End If
        If solution.RepoRoot Is Nothing Then
            entry.Verdict = "no_git"
            entry.Head.Note = "no repository root recorded"
            entry.Reason = "run " & run.Id & " recorded commit " & Short8(run.CommitSha) & " but the map holds no repository root for the solution."
            Return entry
        End If
        Dim facts As RepositoryFacts = RepositoryFacts.Read(solution.RepoRoot, run.CommitSha)
        entry.Head.Sha = facts.HeadSha
        entry.Head.TreeDirty = facts.TreeDirty
        entry.Head.BehindBy = facts.BehindBy
        entry.Head.Note = facts.Note
        If Not facts.Available Then
            entry.Verdict = "no_git"
            entry.Reason = "the repository at '" & solution.RepoRoot & "' could not be read: " & facts.Note & "."
        ElseIf facts.TreeDirty.HasValue AndAlso facts.TreeDirty.Value Then
            entry.Verdict = "dirty"
            entry.Reason = If(facts.IsAncestor AndAlso facts.BehindBy.HasValue AndAlso facts.BehindBy.Value > 0,
                              "HEAD " & Short8(facts.HeadSha) & " is " & Commits(facts.BehindBy.Value) & " past the recorded " & Short8(run.CommitSha) & " and the working tree has uncommitted or untracked changes.",
                              "HEAD " & If(String.Equals(facts.HeadSha, run.CommitSha, StringComparison.Ordinal), "equals the recorded " & Short8(run.CommitSha), Short8(facts.HeadSha) & " does not descend from the recorded " & Short8(run.CommitSha)) & " but the working tree has uncommitted or untracked changes.")
        ElseIf Not facts.IsAncestor Then
            entry.Verdict = "diverged"
            entry.Reason = "the recorded " & Short8(run.CommitSha) & " is not an ancestor of HEAD " & Short8(facts.HeadSha) & If(facts.RecordedFound, "", " (not in this repository)") & "; no count is claimed."
        ElseIf facts.BehindBy.HasValue AndAlso facts.BehindBy.Value > 0 Then
            entry.Verdict = "behind"
            entry.Reason = "HEAD " & Short8(facts.HeadSha) & " is " & Commits(facts.BehindBy.Value) & " past the recorded " & Short8(run.CommitSha) & "; the tree is clean."
        Else
            entry.Verdict = "current"
            entry.Reason = "HEAD equals the recorded " & Short8(run.CommitSha) & " and the tree is clean."
        End If
        Return entry
    End Function

    Private Function Short8(sha As String) As String
        If sha Is Nothing Then Return "?"
        Return If(sha.Length > 8, sha.Substring(0, 8), sha)
    End Function

    Private Function Commits(n As Integer) As String
        Return n.ToString(Globalization.CultureInfo.InvariantCulture) & If(n = 1, " commit", " commits")
    End Function

End Module

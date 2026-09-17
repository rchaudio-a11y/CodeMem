' File: MapStatusReader.vb
' Project: CodeMem.Bridging
' Description: map_status: one entry per map solution with every fact and one verdict in the ruled order; the observed-but-unmapped directories from the extract log (005 FR-418-FR-420, Q5, Q6; research R52, R64).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T015): entries come from solutions rows, not registry rows; map_missing_solution cannot arise and is gone; the
' root asked is the extractor's own answer - repo_root, else the solution file's directory (SolutionScope.Resolve, Q6). notInMap is filled
' by NotInMapReader from T028; until then the list is empty and the error null.
' 2026-09-17 (T028): notInMap filled last - the map facts first, the repositories, then the log (R64).
' 2026-09-17 (T021): ScopeOf added beside RootOf - TargetResolver asks the same scope for containment (one root rule, Q6).

Imports System.IO
Imports CodeMem.Core
Imports CodeMem.Extraction

''' <summary>
''' Verdict order: no_git, dirty, diverged, behind, current. The latest completed run is the one compared (INC4). Also the door
''' extract(stale) consults.
''' </summary>
Public Module MapStatusReader

    ''' <summary>
    ''' Builds the envelope on the call's open map.
    ''' </summary>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="map">The call's open map.</param>
    ''' <returns>The envelope.</returns>
    Public Function Read(config As BridgeConfig, map As MapDatabase) As MapStatusEnvelope
        Dim envelope As MapStatusEnvelope = New MapStatusEnvelope With {
            .ReadAtUtc = BridgeJson.NowUtc(),
            .MapPath = config.MapPath,
            .Entries = New List(Of MapStatusEntryEnvelope)(),
            .NotInMap = New List(Of NotInMapEntryEnvelope)(),
            .NotInMapError = Nothing}
        ' The map facts first, every row's, then the repositories: the repository is never asked while a map read is pending.
        Dim solutions As List(Of SolutionRecord) = SolutionsRepository.ReadAll(map)
        Dim runs As Dictionary(Of Long, RunRecord) = New Dictionary(Of Long, RunRecord)()
        For Each solution As SolutionRecord In solutions
            runs(solution.Id) = ExtractRunsRepository.ReadLatestCompleted(map, solution.Id)
        Next
        For Each solution As SolutionRecord In solutions
            envelope.Entries.Add(EntryOf(solution, runs(solution.Id)))
        Next
        Dim roots As List(Of (Key As String, Root As String)) = New List(Of (Key As String, Root As String))()
        For Each solution As SolutionRecord In solutions
            roots.Add((solution.Key, RootOf(solution)))
        Next
        Dim observed As (Entries As List(Of NotInMapEntryEnvelope), ErrorText As String) = NotInMapReader.Read(roots, ExtractLog.LogPath())
        envelope.NotInMap = observed.Entries
        envelope.NotInMapError = observed.ErrorText
        Return envelope
    End Function

    ''' <summary>
    ''' The root map_status asks and the resolver resolves against: the repository root the run recorded, else the solution file's
    ''' directory - the extractor's own scope rule, one door (SolutionScope.Resolve; spec Q6 as ruled).
    ''' </summary>
    ''' <param name="solution">The map solution.</param>
    ''' <returns>The normalised root with one trailing separator.</returns>
    Public Function RootOf(solution As SolutionRecord) As String
        Return ScopeOf(solution).Root
    End Function

    ''' <summary>
    ''' The scope behind <see cref="RootOf"/>, for the resolver's containment question (T021): the same one answer, asked once.
    ''' </summary>
    ''' <param name="solution">The map solution.</param>
    ''' <returns>The scope.</returns>
    Public Function ScopeOf(solution As SolutionRecord) As SolutionScope
        Return SolutionScope.Resolve(solution.RepoRoot, Path.GetDirectoryName(Path.GetFullPath(solution.LastSeenPath)))
    End Function

    ''' <summary>
    ''' One solution's entry from the map facts already read: the repository's answers, the verdict in the ruled order and its reason.
    ''' </summary>
    ''' <param name="solution">The map solution.</param>
    ''' <param name="run">The latest completed run, or Nothing.</param>
    ''' <returns>The entry.</returns>
    Public Function EntryOf(solution As SolutionRecord, run As RunRecord) As MapStatusEntryEnvelope
        Dim entry As MapStatusEntryEnvelope = New MapStatusEntryEnvelope With {
            .SolutionKey = solution.Key,
            .SolutionId = solution.Id,
            .RepoRoot = solution.RepoRoot,
            .Head = New HeadEnvelope()}
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
        Dim facts As RepositoryFacts = RepositoryFacts.Read(RootOf(solution), run.CommitSha)
        entry.Head.Sha = facts.HeadSha
        entry.Head.TreeDirty = facts.TreeDirty
        entry.Head.BehindBy = facts.BehindBy
        entry.Head.Note = facts.Note
        If Not facts.Available Then
            entry.Verdict = "no_git"
            entry.Reason = "the repository at '" & RootOf(solution) & "' could not be read: " & facts.Note & "."
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

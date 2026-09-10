' File: ExtractionRun.vb
' Project: CodeMem.Extraction
' Description: The orchestrator: the production route every run and every test enters through (plan.md Run Order).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO
Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.Data.Sqlite

''' <summary>
''' Executes one extraction end to end and returns its exit code. Prints the summary line on exit 0 (stdout) and exit 4 (stderr).
''' </summary>
Public Class ExtractionRun

    ''' <summary>
    ''' Runs one extraction: open the map, take the lock, compile, stage, reconcile, validate, publish.
    ''' </summary>
    ''' <param name="options">What to extract and where.</param>
    ''' <param name="seams">Test-only seams, or Nothing (what Main passes).</param>
    ''' <returns>The exit code per contracts/cli.md.</returns>
    Public Shared Function Execute(options As ExtractionOptions, seams As RunSeams) As ExitCode
        Dim startedUtc As String = Timestamps.NowUtc()
        Dim solutionPath As String = Path.GetFullPath(options.SolutionPath)
        Dim dbPath As String = Path.GetFullPath(options.DbPath)
        Dim key As String = If(String.IsNullOrEmpty(options.SolutionKey), Path.GetFileNameWithoutExtension(solutionPath), options.SolutionKey)

        If Not File.Exists(solutionPath) Then
            Console.Error.WriteLine("solution not found: " & solutionPath)
            Return ExitCode.Failure
        End If
        If Not Directory.Exists(Path.GetDirectoryName(dbPath)) Then
            Console.Error.WriteLine("db directory does not exist: " & Path.GetDirectoryName(dbPath))
            Return ExitCode.Failure
        End If

        ' Step 2: open or create; refuse a schema mismatch.
        Dim db As MapDatabase
        Try
            db = MapDatabase.OpenOrCreate(dbPath)
        Catch ex As SchemaVersionMismatchException
            Console.Error.WriteLine(ex.Message)
            Return ExitCode.Failure
        Catch ex As SqliteException When ex.SqliteErrorCode = 8 OrElse ex.SqliteErrorCode = 14
            Console.Error.WriteLine("lock unobtainable: " & ex.Message)
            Return ExitCode.LockHeld
        Catch ex As SqliteException
            Console.Error.WriteLine("database error: " & ex.Message)
            Return ExitCode.Failure
        End Try

        Using db
            ' Step 3: the lock is the write transaction.
            Try
                db.BeginImmediate()
            Catch ex As MapLockHeldException
                Console.Error.WriteLine(ex.Message)
                Return ExitCode.LockHeld
            End Try
            Try
                Return Run(db, options, seams, solutionPath, key, startedUtc)
            Catch ex As WorkspaceLoadException
                db.Rollback()
                Console.Error.WriteLine(ex.Message)
                Return ExitCode.Failure
            Catch ex As DuplicateDocCommentIdException
                db.Rollback()
                Console.Error.WriteLine(ex.Message)
                Return ExitCode.Failure
            Catch ex As SqliteException
                db.Rollback()
                Console.Error.WriteLine("database error: " & ex.Message)
                Return ExitCode.Failure
            Catch ex As Exception
                db.Rollback()
                Console.Error.WriteLine("failure: " & ex.ToString())
                Return ExitCode.Failure
            End Try
        End Using
    End Function

    Private Shared Function Run(db As MapDatabase, options As ExtractionOptions, seams As RunSeams, solutionPath As String, key As String, startedUtc As String) As ExitCode
        Dim abortAt As RunPhase = ReadAbortPhase()

        ' Step 4: identity setup, not a run fact (Article V v1.2.1).
        Dim solution As SolutionRecord = SolutionsRepository.EnsureByKey(db, key, key, solutionPath, Timestamps.NowUtc())
        Dim basePath As String = SolutionPaths.BaseDirectory(solutionPath)

        Using loader As SolutionLoader = SolutionLoader.Open(solutionPath, options.Configuration, options.Framework)
            ' Step 5: the green gate.
            Dim compiled As List(Of CompiledProject) = loader.CompileAll(basePath)
            Dim errorCount As Integer = 0
            For Each project As CompiledProject In compiled
                For Each diagnostic As Diagnostic In project.Errors
                    Console.Error.WriteLine(diagnostic.ToString())
                    errorCount += 1
                Next
            Next
            If errorCount > 0 Then
                Console.Error.WriteLine("errors=" & errorCount)
                db.Rollback()
                Return ExitCode.BuildErrors
            End If

            ' Step 6: one enumeration feeds digest and provenance.
            Dim inputs As List(Of CompiledInput) = CompiledInputs.Enumerate(loader.Solution, basePath)
            Dim digest As String = SourceDigest.Compute(inputs)
            Dim git As GitFacts = GitProvenance.Read(basePath, inputs)
            Dim targetFramework As String = If(String.IsNullOrEmpty(options.Framework), If(compiled.Count > 0, compiled(0).TargetFramework, "none"), options.Framework)
            Dim stamp As RunStamp = New RunStamp With {
                .SolutionId = solution.Id,
                .SourceDigest = digest,
                .CommitSha = git.CommitSha,
                .IsDirty = git.IsDirty,
                .BuildConfiguration = options.Configuration,
                .TargetFramework = targetFramework,
                .ExtractorVersion = ExtractorVersion(),
                .SchemaVersion = SchemaVersion.Current,
                .StartedUtc = startedUtc}

            ' Step 7: stage symbols (one walk per compilation), merge namespaces, add project rows, then run the eight edge rules.
            Dim staged As List(Of ObservedSymbol) = New List(Of ObservedSymbol)()
            Dim perProject As Dictionary(Of CompiledProject, List(Of ObservedSymbol)) = New Dictionary(Of CompiledProject, List(Of ObservedSymbol))()
            Dim treesOf As Dictionary(Of CompiledProject, HashSet(Of SyntaxTree)) = New Dictionary(Of CompiledProject, HashSet(Of SyntaxTree))()
            For Each project As CompiledProject In compiled
                Dim trees As HashSet(Of SyntaxTree) = CompiledInputs.SourceTrees(project.Project, project.Compilation)
                Dim observed As List(Of ObservedSymbol) = SymbolWalker.Walk(project.Compilation, ProjectSymbols.DocIdOf(project.Project), basePath, trees)
                treesOf(project) = trees
                perProject(project) = observed
                staged.AddRange(observed)
            Next
            staged = SymbolWalker.MergeNamespaces(staged)
            For Each project As CompiledProject In compiled
                staged.Add(ProjectSymbols.Create(project.Project, basePath))
            Next
            Dim rowDocIds As HashSet(Of String) = New HashSet(Of String)(StringComparer.Ordinal)
            For Each symbol As ObservedSymbol In staged
                rowDocIds.Add(symbol.DocCommentId)
            Next
            Dim edges As List(Of ObservedEdge) = New List(Of ObservedEdge)()
            Dim treeRules As List(Of EdgeRule) = New List(Of EdgeRule) From {
                New CallsRule(), New UsesRule(), New ImplementsRule(), New ExtendsRule(), New ImportsRule(), New DependsOnRule(), New HandlesRule()}
            For Each project As CompiledProject In compiled
                Dim context As EdgeContext = New EdgeContext(project, loader.Solution, basePath, treesOf(project), perProject(project), rowDocIds)
                For Each rule As EdgeRule In treeRules
                    rule.Collect(context, edges)
                Next
            Next
            Dim partOf As EdgeRule = New PartOfRule()
            partOf.Collect(New EdgeContext(Nothing, loader.Solution, basePath, New HashSet(Of SyntaxTree)(), staged, rowDocIds), edges)
            edges = Canonical(edges)
            If seams IsNot Nothing AndAlso seams.MutateStaged IsNot Nothing Then seams.MutateStaged.Invoke(staged)

            ' Step 8: reconcile against this solution's registry only.
            Dim snapshot As List(Of RegistryRow) = CodeSymbolsRepository.ReadActive(db, solution.Id)
            Dim unmatched As HashSet(Of String) = New HashSet(Of String)(rowDocIds, StringComparer.Ordinal)
            For Each row As RegistryRow In snapshot
                unmatched.Remove(row.DocCommentId)
            Next
            Dim retired As List(Of RegistryRow) = CodeSymbolsRepository.ReadRetiredByDocIds(db, solution.Id, unmatched)
            Dim result As ReconciliationResult = Reconciler.Reconcile(staged, snapshot, retired)

            ' Step 9: test-only seams (I8 count corruption; I9 process abort after staging).
            If seams IsNot Nothing AndAlso seams.CorruptStagedCounts IsNot Nothing Then seams.CorruptStagedCounts.Invoke(result.Counts)
            AbortIf(abortAt, RunPhase.AfterStaging)

            ' Step 10: the residuals are computed by code that never sees the reconciler (FR-025); non-zero fails the run (FR-026).
            Dim counts As RunCounts = CountAuditor.Audit(result.Counts)
            If counts.UnaccountedObserved <> 0 OrElse counts.UnaccountedRegistry <> 0 Then
                stamp.FinishedUtc = Timestamps.NowUtc()
                Dim failedRunId As Long = ExtractRunsRepository.InsertFailed(db, stamp, counts)
                db.Commit()
                Console.Error.WriteLine("outcome=failed " & SummaryLine.Format(key, failedRunId, counts, digest, git.CommitSha))
                Return ExitCode.ResidualMismatch
            End If

            ' Step 11: publish inside the open transaction. The run row is the first fact-table write.
            stamp.FinishedUtc = Timestamps.NowUtc()
            Dim runId As Long = ExtractRunsRepository.InsertCompleted(db, stamp, counts)
            Dim ids As Dictionary(Of String, Long) = New Dictionary(Of String, Long)(result.ExistingIds, StringComparer.Ordinal)
            Dim inserts As List(Of ObservedSymbol) = New List(Of ObservedSymbol)(result.Inserts)
            inserts.Sort(Function(a As ObservedSymbol, b As ObservedSymbol)
                             Dim byRank As Integer = SymbolKindNames.InsertRank(a.Kind).CompareTo(SymbolKindNames.InsertRank(b.Kind))
                             If byRank <> 0 Then Return byRank
                             Return String.CompareOrdinal(a.DocCommentId, b.DocCommentId)
                         End Function)
            For Each symbol As ObservedSymbol In inserts
                ids(symbol.DocCommentId) = CodeSymbolsRepository.InsertNew(db, solution.Id, symbol, Resolve(ids, symbol.ContainerDocCommentId), Resolve(ids, symbol.ProjectDocCommentId), runId)
            Next
            For Each symbol As ObservedSymbol In result.Refreshes
                CodeSymbolsRepository.RefreshMatched(db, ids(symbol.DocCommentId), symbol.Name, Resolve(ids, symbol.ContainerDocCommentId), Resolve(ids, symbol.ProjectDocCommentId), symbol.Primary, symbol.BodyHash, runId)
            Next
            For Each symbol As ObservedSymbol In result.Reactivations
                CodeSymbolsRepository.Reactivate(db, ids(symbol.DocCommentId), symbol.Name, Resolve(ids, symbol.ContainerDocCommentId), Resolve(ids, symbol.ProjectDocCommentId), symbol.Primary, symbol.BodyHash, runId)
            Next
            For Each row As RegistryRow In result.Retirements
                CodeSymbolsRepository.Retire(db, row.Id)
            Next

            Dim parts As List(Of KeyValuePair(Of Long, ObservedPart)) = New List(Of KeyValuePair(Of Long, ObservedPart))()
            For Each symbol As ObservedSymbol In staged
                For Each part As ObservedPart In symbol.Parts
                    parts.Add(New KeyValuePair(Of Long, ObservedPart)(ids(symbol.DocCommentId), part))
                Next
            Next
            parts.Sort(Function(a As KeyValuePair(Of Long, ObservedPart), b As KeyValuePair(Of Long, ObservedPart))
                           Dim byPath As Integer = SolutionPaths.Compare(a.Value.Location.Path, b.Value.Location.Path)
                           If byPath <> 0 Then Return byPath
                           Return a.Value.Location.StartOffset.CompareTo(b.Value.Location.StartOffset)
                       End Function)
            CodePartsRepository.ReplaceForSolution(db, solution.Id, parts)
            AbortIf(abortAt, RunPhase.DuringPublish)
            CodeEdgesRepository.ReplaceForSolution(db, solution.Id, edges, ids)
            For Each candidate As RenameCandidate In result.Candidates
                RenameCandidatesRepository.Insert(db, solution.Id, runId, candidate.RetiredSymbolId, ids(candidate.NewDocCommentId), candidate.BodyHash, candidate.SamePath, candidate.OffsetDistance, candidate.Rank)
            Next

            SolutionsRepository.RefreshLabels(db, solution.Id, git.RepoRoot, solutionPath)
            SolutionsRepository.SetFirstRunIfNull(db, solution.Id, runId)

            ' Step 12.
            db.Commit()
            Console.Out.WriteLine(SummaryLine.Format(key, runId, counts, digest, git.CommitSha))
            Return ExitCode.Success
        End Using
    End Function

    ''' <summary>
    ''' Deduplicates staged edges (an invocation and its member-access child bind the same occurrence) and sorts them by
    ''' (path, offset, verb, target, source, via) so insert order is stable (plan.md determinism guards).
    ''' </summary>
    ''' <param name="edges">The collected edges.</param>
    ''' <returns>The canonical list.</returns>
    Public Shared Function Canonical(edges As IEnumerable(Of ObservedEdge)) As List(Of ObservedEdge)
        Dim seen As HashSet(Of String) = New HashSet(Of String)(StringComparer.Ordinal)
        Dim result As List(Of ObservedEdge) = New List(Of ObservedEdge)()
        For Each edge As ObservedEdge In edges
            Dim key As String = String.Join(ControlChars.NullChar, edge.SourceDocCommentId, EdgeVerbNames.ToText(edge.Verb), edge.TargetDocCommentId, If(edge.ViaDocCommentId, ""), edge.Location.Path, edge.Location.StartOffset, edge.Location.Length)
            If seen.Add(key) Then result.Add(edge)
        Next
        result.Sort(Function(a As ObservedEdge, b As ObservedEdge)
                        Dim c As Integer = SolutionPaths.Compare(a.Location.Path, b.Location.Path)
                        If c <> 0 Then Return c
                        c = a.Location.StartOffset.CompareTo(b.Location.StartOffset)
                        If c <> 0 Then Return c
                        c = String.CompareOrdinal(EdgeVerbNames.ToText(a.Verb), EdgeVerbNames.ToText(b.Verb))
                        If c <> 0 Then Return c
                        c = String.CompareOrdinal(a.TargetDocCommentId, b.TargetDocCommentId)
                        If c <> 0 Then Return c
                        c = String.CompareOrdinal(a.SourceDocCommentId, b.SourceDocCommentId)
                        If c <> 0 Then Return c
                        Return String.CompareOrdinal(If(a.ViaDocCommentId, ""), If(b.ViaDocCommentId, ""))
                    End Function)
        Return result
    End Function

    ''' <summary>
    ''' Reads the test-only abort seam once: CODEMEM_TEST_ABORT_AT = AfterStaging or DuringPublish; anything else is inert (research R11).
    ''' </summary>
    ''' <returns>The phase to abort at, or None.</returns>
    Public Shared Function ReadAbortPhase() As RunPhase
        Dim value As String = Environment.GetEnvironmentVariable("CODEMEM_TEST_ABORT_AT")
        If String.Equals(value, "AfterStaging", StringComparison.Ordinal) Then Return RunPhase.AfterStaging
        If String.Equals(value, "DuringPublish", StringComparison.Ordinal) Then Return RunPhase.DuringPublish
        Return RunPhase.None
    End Function

    Private Shared Sub AbortIf(configured As RunPhase, here As RunPhase)
        If configured = here Then Environment.FailFast("CODEMEM_TEST_ABORT_AT=" & here.ToString())
    End Sub

    Private Shared Function Resolve(ids As Dictionary(Of String, Long), docId As String) As Long?
        If docId Is Nothing Then Return Nothing
        Dim id As Long
        If ids.TryGetValue(docId, id) Then Return id
        Return Nothing
    End Function

    ''' <summary>
    ''' The stamped extractor version: this assembly's version as Major.Minor.Build (research R12).
    ''' </summary>
    ''' <returns>The version text.</returns>
    Public Shared Function ExtractorVersion() As String
        Dim version As Version = GetType(ExtractionRun).Assembly.GetName().Version
        Return version.Major & "." & version.Minor & "." & version.Build
    End Function

End Class

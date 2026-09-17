' File: ExtractionRun.vb
' Project: CodeMem.Extraction
' Description: The orchestrator: the production route every run and every test enters through (plan.md Run Order, revised by fixpack 002).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002): preflight, open, lock, InspectSchema, fresh-map creation and the 1 -> 2 upgrade all sit inside one error
' boundary (F6, F9, FR-106, FR-112, FR-115); MapLockHeldException -> 3, everything else -> 1 with one stderr line; the abort seam needs
' the nonce (F8); the stamp carries sdk_version (FR-109); refreshes and reactivations pass the observed kind (F2).
' 2026-09-13 (fixpack 003, rule 1): step 8 resolves the SolutionScope from the git facts and stages, adds project rows for and runs the tree
' rules over in-scope projects only (FR-201, FR-203); the green gate of step 6 still covers every compiled project.
' 2026-09-17 (feature 005, T009): step 4 gains the 2 -> 3 migration on every path (Fresh: 1 -> 2 -> 3; Version1: 2 -> 3; Version2: 3), the
' DuringUpgrade seam after the last migration statement of whichever path ran (research R63).

Imports System.IO
Imports System.Text.RegularExpressions
Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.Data.Sqlite

''' <summary>
''' Executes one extraction end to end and returns its exit code. Prints the summary line on exit 0 (stdout) and exit 4 (stderr).
''' </summary>
Public Class ExtractionRun

    Private Shared ReadOnly LineBreaks As Regex = New Regex("\r\n|\r|\n", RegexOptions.Compiled)

    ''' <summary>
    ''' Runs one extraction: normalise paths, open the map, take the lock, create or upgrade the schema, compile, stage, reconcile,
    ''' validate, publish. Every failure from the first statement onward maps to an exit code (FR-115).
    ''' </summary>
    ''' <param name="options">What to extract and where.</param>
    ''' <param name="seams">Test-only seams, or Nothing (what Main passes).</param>
    ''' <returns>The exit code per contracts/cli.md.</returns>
    Public Shared Function Execute(options As ExtractionOptions, seams As RunSeams) As ExitCode
        Dim startedUtc As String = Timestamps.NowUtc()
        Try
            ' Step 1: preflight, inside the boundary (F9).
            Dim solutionPath As String = Path.GetFullPath(options.SolutionPath)
            Dim dbPath As String = Path.GetFullPath(options.DbPath)
            Dim key As String = If(String.IsNullOrEmpty(options.SolutionKey), Path.GetFileNameWithoutExtension(solutionPath), options.SolutionKey)
            If Not File.Exists(solutionPath) Then Return Refuse(ExitCode.Failure, "solution not found: " & solutionPath)
            If Directory.Exists(dbPath) Then Return Refuse(ExitCode.Failure, "db path is a directory: " & dbPath)
            If Not Directory.Exists(Path.GetDirectoryName(dbPath)) Then Return Refuse(ExitCode.Failure, "db directory does not exist: " & Path.GetDirectoryName(dbPath))

            ' Step 2: open (typed connection string, no schema work).
            Using db As MapDatabase = MapDatabase.Open(dbPath)
                ' Step 3: the lock is the write transaction.
                db.BeginImmediate()
                Dim abortAt As RunPhase = ReadAbortPhase()

                ' Step 4: one door decides fresh / version 1 / version 2 / current / foreign / newer (FR-105, FR-112, FR-114; 005 FR-429).
                Dim found As Integer
                Select Case db.InspectSchema(found)
                    Case SchemaState.Fresh
                        SchemaRepository.CreateVersion1(db)
                        MapIdentityRepository.Insert(db, Guid.NewGuid().ToString(), 1, Timestamps.NowUtc())
                        AbortIf(abortAt, RunPhase.DuringInitialize)
                        SchemaRepository.UpgradeToVersion2(db)
                        SchemaRepository.UpgradeToVersion3(db)
                        AbortIf(abortAt, RunPhase.DuringUpgrade)
                        SchemaRepository.SetSchemaVersion(db, SchemaVersion.Current)
                    Case SchemaState.Version1
                        SchemaRepository.UpgradeToVersion2(db)
                        SchemaRepository.UpgradeToVersion3(db)
                        AbortIf(abortAt, RunPhase.DuringUpgrade)
                        SchemaRepository.SetSchemaVersion(db, SchemaVersion.Current)
                    Case SchemaState.Version2
                        SchemaRepository.UpgradeToVersion3(db)
                        AbortIf(abortAt, RunPhase.DuringUpgrade)
                        SchemaRepository.SetSchemaVersion(db, SchemaVersion.Current)
                    Case SchemaState.Current
                        ' nothing to do
                    Case SchemaState.Foreign
                        Throw New NotAMapException(dbPath)
                    Case Else
                        Throw New SchemaVersionMismatchException(found, SchemaVersion.Current)
                End Select

                Return Run(db, options, seams, solutionPath, key, startedUtc, abortAt)
            End Using
        Catch ex As MapLockHeldException
            Return Refuse(ExitCode.LockHeld, ex.Message)
        Catch ex As Exception
            Return Refuse(ExitCode.Failure, OneLine(ex))
        End Try
    End Function

    Private Shared Function Run(db As MapDatabase, options As ExtractionOptions, seams As RunSeams, solutionPath As String, key As String, startedUtc As String, abortAt As RunPhase) As ExitCode
        ' Step 5: identity setup, not a run fact (Article V v1.2.1).
        Dim solution As SolutionRecord = SolutionsRepository.EnsureByKey(db, key, key, solutionPath, Timestamps.NowUtc())
        Dim basePath As String = SolutionPaths.BaseDirectory(solutionPath)

        Using loader As SolutionLoader = SolutionLoader.Open(solutionPath, options.Configuration, options.Framework)
            ' Step 6: the green gate.
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

            ' Step 7: one enumeration feeds digest and provenance; the SDK stamp is read in-process (FR-109).
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
                .SdkVersion = SdkVersion.Resolve(basePath),
                .StartedUtc = startedUtc}

            ' Step 8: resolve the scope root (fixpack 003, FR-201: the repository working directory the stamp carries, else the solution
            ' directory), then stage symbols (one walk per in-scope compilation), merge namespaces, add project rows for in-scope projects,
            ' then run the eight edge rules. A project whose file is outside the scope root contributes nothing (FR-203).
            Dim scope As SolutionScope = SolutionScope.Resolve(git.RepoRoot, basePath)
            Dim inScope As List(Of CompiledProject) = compiled.FindAll(Function(p As CompiledProject) scope.Contains(p.Project.FilePath))
            Dim staged As List(Of ObservedSymbol) = New List(Of ObservedSymbol)()
            Dim perProject As Dictionary(Of CompiledProject, List(Of ObservedSymbol)) = New Dictionary(Of CompiledProject, List(Of ObservedSymbol))()
            Dim treesOf As Dictionary(Of CompiledProject, HashSet(Of SyntaxTree)) = New Dictionary(Of CompiledProject, HashSet(Of SyntaxTree))()
            For Each project As CompiledProject In inScope
                Dim trees As HashSet(Of SyntaxTree) = CompiledInputs.SourceTrees(project.Project, project.Compilation, scope)
                Dim observed As List(Of ObservedSymbol) = SymbolWalker.Walk(project.Compilation, ProjectSymbols.DocIdOf(project.Project), basePath, trees)
                treesOf(project) = trees
                perProject(project) = observed
                staged.AddRange(observed)
            Next
            staged = SymbolWalker.MergeNamespaces(staged)
            For Each project As CompiledProject In inScope
                staged.Add(ProjectSymbols.Create(project.Project, basePath))
            Next
            Dim rowDocIds As HashSet(Of String) = New HashSet(Of String)(StringComparer.Ordinal)
            For Each symbol As ObservedSymbol In staged
                rowDocIds.Add(symbol.DocCommentId)
            Next
            Dim edges As List(Of ObservedEdge) = New List(Of ObservedEdge)()
            Dim treeRules As List(Of EdgeRule) = New List(Of EdgeRule) From {
                New CallsRule(), New UsesRule(), New ImplementsRule(), New ExtendsRule(), New ImportsRule(), New DependsOnRule(), New HandlesRule()}
            For Each project As CompiledProject In inScope
                Dim context As EdgeContext = New EdgeContext(project, loader.Solution, basePath, treesOf(project), perProject(project), rowDocIds)
                For Each rule As EdgeRule In treeRules
                    rule.Collect(context, edges)
                Next
            Next
            Dim partOf As EdgeRule = New PartOfRule()
            partOf.Collect(New EdgeContext(Nothing, loader.Solution, basePath, New HashSet(Of SyntaxTree)(), staged, rowDocIds), edges)
            edges = Canonical(edges)
            If seams IsNot Nothing AndAlso seams.MutateStaged IsNot Nothing Then seams.MutateStaged.Invoke(staged)

            ' Step 9: reconcile against this solution's registry only.
            Dim snapshot As List(Of RegistryRow) = CodeSymbolsRepository.ReadActive(db, solution.Id)
            Dim unmatched As HashSet(Of String) = New HashSet(Of String)(rowDocIds, StringComparer.Ordinal)
            For Each row As RegistryRow In snapshot
                unmatched.Remove(row.DocCommentId)
            Next
            Dim retired As List(Of RegistryRow) = CodeSymbolsRepository.ReadRetiredByDocIds(db, solution.Id, unmatched)
            Dim result As ReconciliationResult = Reconciler.Reconcile(staged, snapshot, retired)

            ' Step 10: test-only seams (I8 count corruption; I9 process abort after staging).
            If seams IsNot Nothing AndAlso seams.CorruptStagedCounts IsNot Nothing Then seams.CorruptStagedCounts.Invoke(result.Counts)
            AbortIf(abortAt, RunPhase.AfterStaging)

            ' Step 11: the residuals are computed by code that never sees the reconciler (FR-025); non-zero fails the run (FR-026).
            Dim counts As RunCounts = CountAuditor.Audit(result.Counts)
            If counts.UnaccountedObserved <> 0 OrElse counts.UnaccountedRegistry <> 0 Then
                stamp.FinishedUtc = Timestamps.NowUtc()
                Dim failedRunId As Long = ExtractRunsRepository.InsertFailed(db, stamp, counts)
                db.Commit()
                Console.Error.WriteLine("outcome=failed " & SummaryLine.Format(key, failedRunId, counts, digest, git.CommitSha))
                Return ExitCode.ResidualMismatch
            End If

            ' Step 12: publish inside the open transaction. The run row is the first fact-table write.
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
                CodeSymbolsRepository.RefreshMatched(db, ids(symbol.DocCommentId), symbol.Kind, symbol.Name, Resolve(ids, symbol.ContainerDocCommentId), Resolve(ids, symbol.ProjectDocCommentId), symbol.Primary, symbol.BodyHash, runId)
            Next
            For Each symbol As ObservedSymbol In result.Reactivations
                CodeSymbolsRepository.Reactivate(db, ids(symbol.DocCommentId), symbol.Kind, symbol.Name, Resolve(ids, symbol.ContainerDocCommentId), Resolve(ids, symbol.ProjectDocCommentId), symbol.Primary, symbol.BodyHash, runId)
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

            ' Step 13.
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
    ''' Reads the test-only abort seam once (research R26, FR-116). <c>CODEMEM_TEST_ABORT_AT</c> must read <c>&lt;phase&gt;:&lt;nonce&gt;</c>
    ''' where the phase is a known <see cref="RunPhase"/> other than None, and <c>CODEMEM_TEST_NONCE</c> must be set, non-empty and
    ''' ordinal-equal to <c>&lt;nonce&gt;</c>. Either variable alone, a phase without a nonce, an unknown phase or unequal nonces is inert.
    ''' </summary>
    ''' <returns>The phase to abort at, or None.</returns>
    Public Shared Function ReadAbortPhase() As RunPhase
        Dim value As String = Environment.GetEnvironmentVariable("CODEMEM_TEST_ABORT_AT")
        Dim nonce As String = Environment.GetEnvironmentVariable("CODEMEM_TEST_NONCE")
        If String.IsNullOrEmpty(value) OrElse String.IsNullOrEmpty(nonce) Then Return RunPhase.None
        Dim separator As Integer = value.IndexOf(":"c)
        If separator < 0 Then Return RunPhase.None
        Dim expected As String = value.Substring(separator + 1)
        If expected.Length = 0 OrElse Not String.Equals(expected, nonce, StringComparison.Ordinal) Then Return RunPhase.None
        Select Case value.Substring(0, separator)
            Case "DuringInitialize" : Return RunPhase.DuringInitialize
            Case "DuringUpgrade" : Return RunPhase.DuringUpgrade
            Case "AfterStaging" : Return RunPhase.AfterStaging
            Case "DuringPublish" : Return RunPhase.DuringPublish
            Case Else : Return RunPhase.None
        End Select
    End Function

    Private Shared Sub AbortIf(configured As RunPhase, here As RunPhase)
        If configured = here Then Environment.FailFast("CODEMEM_TEST_ABORT_AT=" & here.ToString())
    End Sub

    Private Shared Function Refuse(code As ExitCode, message As String) As ExitCode
        Console.Error.WriteLine(message)
        Return code
    End Function

    ''' <summary>
    ''' The one stderr line of an exit-1 refusal (FR-115): the exception's message with line breaks folded to " | "; a SqliteException is
    ''' prefixed "database error: "; an exception outside the run's own vocabulary is prefixed with its type name so the cause stays visible.
    ''' </summary>
    ''' <param name="ex">The failure.</param>
    ''' <returns>One line, no stack frames.</returns>
    Public Shared Function OneLine(ex As Exception) As String
        Dim text As String
        If TypeOf ex Is SqliteException Then
            text = "database error: " & ex.Message
        ElseIf TypeOf ex Is WorkspaceLoadException OrElse TypeOf ex Is DuplicateDocCommentIdException OrElse TypeOf ex Is SchemaVersionMismatchException OrElse
               TypeOf ex Is NotAMapException OrElse TypeOf ex Is SdkResolutionException OrElse TypeOf ex Is ArgumentException OrElse
               TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException Then
            text = ex.Message
        Else
            text = ex.GetType().Name & ": " & ex.Message
        End If
        Return LineBreaks.Replace(text, " | ")
    End Function

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

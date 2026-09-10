' File: SchemaConstraintTests.vb
' Project: CodeMem.Tests
' Description: The schema's rules fire in real SQLite (Article XII) and a constraint failure propagates through the production route as exit 1 naming the constraint (Article XIII).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' These tests are their own fire demonstration: each insert is the injected defect and the constraint is the guard.
' RED:   n/a - each insert is the injected defect; the CHECK text 'kind IN ('namespace', 'project')' was confirmed present in SQLite's message.
' GREEN: 2026-09-09 first run, 2 of 2 (direct inserts and the seam-driven production route).

Imports System.IO
Imports CodeMem.Core
Imports CodeMem.Extraction
Imports Microsoft.Data.Sqlite
Imports Xunit

''' <summary>
''' Direct inserts against a real map, then the seam-driven production-route proof. This file is allowed SQL by the location gate.
''' </summary>
<Collection("Fixture")>
Public Class SchemaConstraintTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' Each forbidden insert throws a SqliteException naming its constraint; a retired duplicate identity is allowed.
    ''' </summary>
    <Fact>
    Public Sub ConstraintsFireInSqlite()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim runId As Long = MapQueries.ReadRuns(map.Path)(0).Id
            Dim symbols As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
            Dim aClass As SymbolRow = symbols.Find(Function(s As SymbolRow) s.Kind = "class")
            Dim aProject As SymbolRow = symbols.Find(Function(s As SymbolRow) s.Kind = "project")

            Using connection As SqliteConnection = New SqliteConnection("Data Source=" & map.Path & ";Pooling=False")
                connection.Open()
                Execute(connection, "PRAGMA foreign_keys = ON")

                ExpectConstraint(connection, "id = 1",
                    "INSERT INTO map_identity (id, map_guid, schema_version, created_utc) VALUES (2, 'x', 1, 'now')")
                ExpectConstraint(connection, "solutions.key",
                    "INSERT INTO solutions (key, name, last_seen_path, created_utc) VALUES ('Sample', 'dup', 'p', 'now')")
                ExpectConstraint(connection, "is_dirty",
                    "INSERT INTO extract_runs (solution_id, outcome, source_digest, commit_sha, is_dirty, build_configuration, target_framework, extractor_version, schema_version, started_utc, finished_utc, symbols_observed, symbols_matched, symbols_reactivated, symbols_new, symbols_retired, registry_active_before, notes_orphaned, rename_candidates, unaccounted_observed, unaccounted_registry) " &
                    "VALUES (" & solutionId & ", 'completed', 'd', 'abc', NULL, 'Debug', 'net8.0', '0.1.0', 1, 'now', 'now', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)")
                ExpectConstraint(connection, "code_symbols.solution_id, code_symbols.doc_comment_id",
                    SymbolInsert(solutionId, aClass.DocCommentId, "class", aProject.Id, 1, runId))
                ExpectConstraint(connection, "kind IN ('namespace', 'project')",
                    SymbolInsert(solutionId, "T:Sample.NoProject", "class", Nothing, 1, runId))
                ExpectConstraint(connection, "kind IN ('namespace', 'project')",
                    SymbolInsert(solutionId, "Project:Extra", "project", aProject.Id, 1, runId))
                ExpectConstraint(connection, "via_symbol_id IS NULL OR verb = 'handles'",
                    "INSERT INTO code_edges (solution_id, source_symbol_id, verb, target_symbol_id, target_doc_comment_id, via_symbol_id, path, start_offset, length, start_line, start_column) " &
                    "VALUES (" & solutionId & ", " & aClass.Id & ", 'calls', NULL, 'M:X.Y', " & aClass.Id & ", 'p', 0, 1, 1, 1)")
                ExpectConstraint(connection, "verb IN (",
                    "INSERT INTO code_edges (solution_id, source_symbol_id, verb, target_symbol_id, target_doc_comment_id, via_symbol_id, path, start_offset, length, start_line, start_column) " &
                    "VALUES (" & solutionId & ", " & aClass.Id & ", 'produces', NULL, 'M:X.Y', NULL, 'p', 0, 1, 1, 1)")
                ExpectConstraint(connection, "FOREIGN KEY",
                    SymbolInsert(solutionId, "T:Sample.Orphan", "class", aProject.Id, 1, 999999))

                ' A retired row may share an identity with the active row: the partial unique index covers active rows only.
                Execute(connection, SymbolInsert(solutionId, aClass.DocCommentId, "class", aProject.Id, 0, runId))
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Through the production route: a class staged with no project (the seam) fails the CHECK, the run exits 1, no run row is written, and stderr names the constraint.
    ''' </summary>
    <Fact>
    Public Sub ConstraintFailurePropagatesAsExitOneNamingTheConstraint()
        Using map As TempMap = New TempMap()
            Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}
            Dim seams As RunSeams = New RunSeams With {
                .MutateStaged = Sub(staged As List(Of ObservedSymbol))
                                    Dim victim As ObservedSymbol = staged.Find(Function(s As ObservedSymbol) s.Kind = CodeMem.Core.SymbolKind.Class_)
                                    victim.ProjectDocCommentId = Nothing
                                End Sub}
            Dim original As TextWriter = Console.Error
            Dim captured As StringWriter = New StringWriter()
            Dim code As ExitCode
            Console.SetError(captured)
            Try
                code = ExtractionRun.Execute(options, seams)
            Finally
                Console.SetError(original)
            End Try
            Assert.Equal(ExitCode.Failure, code)
            Assert.Empty(MapQueries.ReadRuns(map.Path))
            Assert.Contains("kind IN ('namespace', 'project')", captured.ToString())
        End Using
    End Sub

    Private Shared Function SymbolInsert(solutionId As Long, docId As String, kind As String, projectSymbolId As Long?, isActive As Integer, runId As Long) As String
        Dim projectValue As String = If(projectSymbolId.HasValue, projectSymbolId.Value.ToString(), "NULL")
        Return "INSERT INTO code_symbols (solution_id, doc_comment_id, kind, name, container_id, project_symbol_id, path, start_offset, length, start_line, start_column, body_hash, is_active, first_seen_run_id, last_seen_run_id) " &
            "VALUES (" & solutionId & ", '" & docId & "', '" & kind & "', 'n', NULL, " & projectValue & ", 'p', 0, 1, 1, 1, 'h', " & isActive & ", " & runId & ", " & runId & ")"
    End Function

    Private Shared Sub Execute(connection As SqliteConnection, sql As String)
        Using command As SqliteCommand = connection.CreateCommand()
            command.CommandText = sql
            command.ExecuteNonQuery()
        End Using
    End Sub

    Private Shared Sub ExpectConstraint(connection As SqliteConnection, constraintText As String, sql As String)
        Dim ex As SqliteException = Assert.Throws(Of SqliteException)(Sub() Execute(connection, sql))
        Assert.Contains("constraint", ex.Message, StringComparison.OrdinalIgnoreCase)
        Assert.Contains(constraintText, ex.Message)
    End Sub

End Class

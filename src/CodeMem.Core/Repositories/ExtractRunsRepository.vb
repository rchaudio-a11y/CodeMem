' File: ExtractRunsRepository.vb
' Project: CodeMem.Core
' Description: Inserts the extract_runs stamp with all ten counts, completed or failed (Articles V, VIII).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002): binds sdk_version (NULL when the stamp has none). The trigger, not this code, refuses a v2 row without it (Article XII).
' 2026-09-15 (feature 004, T018): ReadLatest (any outcome, INC4), ReadLatestCompleted and ReadById for the bridge, SELECT-only (research R45).

Imports Microsoft.Data.Sqlite

''' <summary>
''' Writes run rows. Every column of the stamp and every count is bound; nothing is defaulted.
''' </summary>
Public Module ExtractRunsRepository

    Private Const ReadColumns As String = "id, solution_id, outcome, source_digest, commit_sha, is_dirty, build_configuration, target_framework, extractor_version, schema_version, started_utc, finished_utc, symbols_observed, symbols_matched, symbols_reactivated, symbols_new, symbols_retired, registry_active_before, notes_orphaned, rename_candidates, unaccounted_observed, unaccounted_registry, sdk_version"

    ''' <summary>
    ''' Inserts a completed run row.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="stamp">The provenance stamp.</param>
    ''' <param name="counts">The ten audited counts.</param>
    ''' <returns>The new run id.</returns>
    Public Function InsertCompleted(db As MapDatabase, stamp As RunStamp, counts As RunCounts) As Long
        Return Insert(db, "completed", stamp, counts)
    End Function

    ''' <summary>
    ''' Inserts a failed run row (residual mismatch, exit 4).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="stamp">The provenance stamp.</param>
    ''' <param name="counts">The ten audited counts, at least one residual non-zero.</param>
    ''' <returns>The new run id.</returns>
    Public Function InsertFailed(db As MapDatabase, stamp As RunStamp, counts As RunCounts) As Long
        Return Insert(db, "failed", stamp, counts)
    End Function

    Private Function Insert(db As MapDatabase, outcome As String, stamp As RunStamp, counts As RunCounts) As Long
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "INSERT INTO extract_runs (solution_id, outcome, source_digest, commit_sha, is_dirty, build_configuration, target_framework, extractor_version, schema_version, started_utc, finished_utc, " &
                "symbols_observed, symbols_matched, symbols_reactivated, symbols_new, symbols_retired, registry_active_before, notes_orphaned, rename_candidates, unaccounted_observed, unaccounted_registry, sdk_version) " &
                "VALUES (@solution_id, @outcome, @source_digest, @commit_sha, @is_dirty, @build_configuration, @target_framework, @extractor_version, @schema_version, @started_utc, @finished_utc, " &
                "@symbols_observed, @symbols_matched, @symbols_reactivated, @symbols_new, @symbols_retired, @registry_active_before, @notes_orphaned, @rename_candidates, @unaccounted_observed, @unaccounted_registry, @sdk_version); " &
                "SELECT last_insert_rowid()"
            command.Parameters.AddWithValue("@solution_id", stamp.SolutionId)
            command.Parameters.AddWithValue("@outcome", outcome)
            command.Parameters.AddWithValue("@source_digest", stamp.SourceDigest)
            command.Parameters.AddWithValue("@commit_sha", If(stamp.CommitSha Is Nothing, CObj(DBNull.Value), CObj(stamp.CommitSha)))
            command.Parameters.AddWithValue("@is_dirty", If(stamp.IsDirty.HasValue, CObj(If(stamp.IsDirty.Value, 1, 0)), CObj(DBNull.Value)))
            command.Parameters.AddWithValue("@build_configuration", stamp.BuildConfiguration)
            command.Parameters.AddWithValue("@target_framework", stamp.TargetFramework)
            command.Parameters.AddWithValue("@extractor_version", stamp.ExtractorVersion)
            command.Parameters.AddWithValue("@schema_version", stamp.SchemaVersion)
            command.Parameters.AddWithValue("@started_utc", stamp.StartedUtc)
            command.Parameters.AddWithValue("@finished_utc", stamp.FinishedUtc)
            command.Parameters.AddWithValue("@symbols_observed", counts.SymbolsObserved)
            command.Parameters.AddWithValue("@symbols_matched", counts.SymbolsMatched)
            command.Parameters.AddWithValue("@symbols_reactivated", counts.SymbolsReactivated)
            command.Parameters.AddWithValue("@symbols_new", counts.SymbolsNew)
            command.Parameters.AddWithValue("@symbols_retired", counts.SymbolsRetired)
            command.Parameters.AddWithValue("@registry_active_before", counts.RegistryActiveBefore)
            command.Parameters.AddWithValue("@notes_orphaned", counts.NotesOrphaned)
            command.Parameters.AddWithValue("@rename_candidates", counts.RenameCandidates)
            command.Parameters.AddWithValue("@unaccounted_observed", counts.UnaccountedObserved)
            command.Parameters.AddWithValue("@unaccounted_registry", counts.UnaccountedRegistry)
            command.Parameters.AddWithValue("@sdk_version", If(stamp.SdkVersion Is Nothing, CObj(DBNull.Value), CObj(stamp.SdkVersion)))
            Return CLng(command.ExecuteScalar())
        End Using
    End Function

    ''' <summary>
    ''' The newest run of a solution, whatever its outcome (what solutions reports; INC4).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <returns>The row, or Nothing when the solution has no run.</returns>
    Public Function ReadLatest(db As MapDatabase, solutionId As Long) As RunRecord
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & ReadColumns & " FROM extract_runs WHERE solution_id = @solution_id ORDER BY id DESC LIMIT 1"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            Return ReadOne(command)
        End Using
    End Function

    ''' <summary>
    ''' The newest completed run of a solution (what map_status and extract compare; INC4).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <returns>The row, or Nothing when no run completed.</returns>
    Public Function ReadLatestCompleted(db As MapDatabase, solutionId As Long) As RunRecord
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & ReadColumns & " FROM extract_runs WHERE solution_id = @solution_id AND outcome = 'completed' ORDER BY id DESC LIMIT 1"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            Return ReadOne(command)
        End Using
    End Function

    ''' <summary>
    ''' One run by id (the row the extractor's summary line names).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="runId">The run id.</param>
    ''' <returns>The row, or Nothing.</returns>
    Public Function ReadById(db As MapDatabase, runId As Long) As RunRecord
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & ReadColumns & " FROM extract_runs WHERE id = @id"
            command.Parameters.AddWithValue("@id", runId)
            Return ReadOne(command)
        End Using
    End Function

    Private Function ReadOne(command As SqliteCommand) As RunRecord
        Using reader As SqliteDataReader = command.ExecuteReader()
            If Not reader.Read() Then Return Nothing
            Dim record As RunRecord = New RunRecord With {
                .Id = reader.GetInt64(0),
                .SolutionId = reader.GetInt64(1),
                .Outcome = reader.GetString(2),
                .SourceDigest = reader.GetString(3),
                .CommitSha = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                .BuildConfiguration = reader.GetString(6),
                .TargetFramework = reader.GetString(7),
                .ExtractorVersion = reader.GetString(8),
                .SchemaVersion = reader.GetInt32(9),
                .StartedUtc = reader.GetString(10),
                .FinishedUtc = reader.GetString(11),
                .SdkVersion = If(reader.IsDBNull(22), Nothing, reader.GetString(22))}
            If Not reader.IsDBNull(5) Then record.IsDirty = reader.GetInt32(5) = 1
            record.Counts = New RunCounts With {
                .SymbolsObserved = reader.GetInt32(12),
                .SymbolsMatched = reader.GetInt32(13),
                .SymbolsReactivated = reader.GetInt32(14),
                .SymbolsNew = reader.GetInt32(15),
                .SymbolsRetired = reader.GetInt32(16),
                .RegistryActiveBefore = reader.GetInt32(17),
                .NotesOrphaned = reader.GetInt32(18),
                .RenameCandidates = reader.GetInt32(19),
                .UnaccountedObserved = reader.GetInt32(20),
                .UnaccountedRegistry = reader.GetInt32(21)}
            Return record
        End Using
    End Function

End Module

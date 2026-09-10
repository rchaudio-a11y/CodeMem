' File: ExtractRunsRepository.vb
' Project: CodeMem.Core
' Description: Inserts the extract_runs stamp with all ten counts, completed or failed (Articles V, VIII).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Microsoft.Data.Sqlite

''' <summary>
''' Writes run rows. Every column of the stamp and every count is bound; nothing is defaulted.
''' </summary>
Public Module ExtractRunsRepository

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
                "symbols_observed, symbols_matched, symbols_reactivated, symbols_new, symbols_retired, registry_active_before, notes_orphaned, rename_candidates, unaccounted_observed, unaccounted_registry) " &
                "VALUES (@solution_id, @outcome, @source_digest, @commit_sha, @is_dirty, @build_configuration, @target_framework, @extractor_version, @schema_version, @started_utc, @finished_utc, " &
                "@symbols_observed, @symbols_matched, @symbols_reactivated, @symbols_new, @symbols_retired, @registry_active_before, @notes_orphaned, @rename_candidates, @unaccounted_observed, @unaccounted_registry); " &
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
            Return CLng(command.ExecuteScalar())
        End Using
    End Function

End Module

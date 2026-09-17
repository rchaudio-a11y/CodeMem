' File: SummaryLine.vb
' Project: CodeMem.Extraction
' Description: The one-line run summary of contracts/cli.md (FR-033), printed on exit 0 and, prefixed, on exit 4.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-17 (feature 005, T035): one trailing field, warnings=<n> - the rows written for the run (contracts/extractor.md §4).

Imports System.Globalization
Imports CodeMem.Core

''' <summary>
''' Formats the summary line. Lives beside the run because the exit-4 path prints it from inside the run.
''' </summary>
Public Module SummaryLine

    ''' <summary>
    ''' Formats the fixed-order key=value line.
    ''' </summary>
    ''' <param name="solutionKey">The solution key.</param>
    ''' <param name="runId">The run id.</param>
    ''' <param name="counts">The ten counts.</param>
    ''' <param name="digest">The source digest.</param>
    ''' <param name="commitSha">The commit sha, or Nothing.</param>
    ''' <param name="warnings">The number of extract_run_warnings rows written for the run (005).</param>
    ''' <returns>The line without a trailing newline.</returns>
    Public Function Format(solutionKey As String, runId As Long, counts As RunCounts, digest As String, commitSha As String, warnings As Integer) As String
        Dim c As CultureInfo = CultureInfo.InvariantCulture
        Return "solution=" & solutionKey &
            " run_id=" & runId.ToString(c) &
            " observed=" & counts.SymbolsObserved.ToString(c) &
            " matched=" & counts.SymbolsMatched.ToString(c) &
            " reactivated=" & counts.SymbolsReactivated.ToString(c) &
            " new=" & counts.SymbolsNew.ToString(c) &
            " retired=" & counts.SymbolsRetired.ToString(c) &
            " registry_before=" & counts.RegistryActiveBefore.ToString(c) &
            " notes_orphaned=" & counts.NotesOrphaned.ToString(c) &
            " candidates=" & counts.RenameCandidates.ToString(c) &
            " unaccounted_observed=" & counts.UnaccountedObserved.ToString(c) &
            " unaccounted_registry=" & counts.UnaccountedRegistry.ToString(c) &
            " digest=" & digest &
            " sha=" & If(commitSha Is Nothing, "null", commitSha) &
            " warnings=" & warnings.ToString(c)
    End Function

End Module

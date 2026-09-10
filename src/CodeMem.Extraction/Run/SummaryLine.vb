' File: SummaryLine.vb
' Project: CodeMem.Extraction
' Description: The one-line run summary of contracts/cli.md (FR-033), printed on exit 0 and, prefixed, on exit 4.
' Author: RCH Automation LLC
' Created: 2026-09-09

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
    ''' <returns>The line without a trailing newline.</returns>
    Public Function Format(solutionKey As String, runId As Long, counts As RunCounts, digest As String, commitSha As String) As String
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
            " sha=" & If(commitSha Is Nothing, "null", commitSha)
    End Function

End Module

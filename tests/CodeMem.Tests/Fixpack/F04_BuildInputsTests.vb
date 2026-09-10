' File: F04_BuildInputsTests.vb
' Project: CodeMem.Tests
' Description: F4 (cheap half) - the four well-known build files walked upward from the solution directory are compiled inputs: they change the digest and not the fact set (FR-107, FR-110, SC-105).
' Author: RCH Automation LLC
' Created: 2026-09-10
'
' RED:   2026-09-10 run 2 red: source_digest equal to run 1 after Directory.Build.props was placed above the copy (the file was not an input).
' GREEN: 2026-09-10 after CompiledInputs.BuildFiles joined Enumerate (T037): runs 2 and 3 differ in digest, S|/P|/E| facts equal, is_dirty null.
' FIRE:  2026-09-10 the walk stopped at the solution directory (dir = Nothing instead of dir.Parent) -> run 2 red (digests equal); reverted -> green.

Imports System.IO
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Build files are placed in FixtureCopy.ParentDirectory (above the copied solution) and inside the solution directory (research R28).
''' </summary>
<Collection("Fixture")>
Public Class F04_BuildInputsTests

    ''' <summary>
    ''' Run 1 baseline; run 2 after an empty Directory.Build.props above the copy: digest differs, S|/P|/E| fact lines equal; run 3 after
    ''' Directory.Build.targets, Directory.Packages.props and global.json above plus a second Directory.Build.props inside: digest differs
    ''' again, facts equal, is_dirty still null (the copy is outside any repository).
    ''' </summary>
    <Fact>
    Public Sub BuildFilesAboveTheSolutionChangeTheDigestAndNotTheFacts()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim facts1 As List(Of String) = Facts(map.Path, solutionId)
                Dim digest1 As String = MapQueries.ReadRuns(map.Path)(0).SourceDigest

                File.WriteAllText(Path.Combine(copy.ParentDirectory, "Directory.Build.props"), "<Project />")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim run2 As RunRow = MapQueries.ReadRuns(map.Path)(1)
                Assert.NotEqual(digest1, run2.SourceDigest)
                Assert.Equal(facts1, Facts(map.Path, solutionId))
                Assert.Equal(run2.SymbolsObserved, run2.SymbolsMatched)

                File.WriteAllText(Path.Combine(copy.ParentDirectory, "Directory.Build.targets"), "<Project />")
                File.WriteAllText(Path.Combine(copy.ParentDirectory, "Directory.Packages.props"), "<Project />")
                File.WriteAllText(Path.Combine(copy.ParentDirectory, "global.json"), "{}")
                File.WriteAllText(Path.Combine(copy.Directory, "Directory.Build.props"), "<Project />")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim run3 As RunRow = MapQueries.ReadRuns(map.Path)(2)
                Assert.NotEqual(run2.SourceDigest, run3.SourceDigest)
                Assert.NotEqual(digest1, run3.SourceDigest)
                Assert.Equal(facts1, Facts(map.Path, solutionId))
                Assert.Null(run3.IsDirty)
                Assert.Null(run3.CommitSha)
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' The S|, P| and E| lines of the canonical fact set (the C| counts line changes legitimately: run 1 is all-new, later runs all-matched).
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="solutionId">Solution id.</param>
    ''' <returns>The fact lines without the counts line.</returns>
    Private Shared Function Facts(db As String, solutionId As Long) As List(Of String)
        Return MapQueries.FactSet(db, solutionId).FindAll(Function(l As String) Not l.StartsWith("C|", StringComparison.Ordinal))
    End Function

End Class

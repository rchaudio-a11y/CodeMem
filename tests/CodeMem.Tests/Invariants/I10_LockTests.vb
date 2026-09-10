' File: I10_LockTests.vb
' Project: CodeMem.Tests
' Description: I10 - a held map is refused at once: in-process under 1 s, the executable with exit 3 under 2 s, nothing written (SC-008).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 both halves hung: DefaultTimeout = 0 waits forever in Microsoft.Data.Sqlite (research R6 verified false) - reported to the Architect.
' GREEN: 2026-09-09 after BEGIN IMMEDIATE moved to the native handle with busy timeout 0 (T090); in-process refusal ~1 ms, executable exit 3 in ~0.3 s.
' FIRE:  2026-09-09 sqlite3_busy_timeout(handle, 5000) -> both halves red (refusals took 5.6 s); reverted -> green.

Imports System.Diagnostics
Imports CodeMem.Core
Imports Xunit

''' <summary>
''' The lock is the BEGIN IMMEDIATE transaction with a zero busy timeout (research R6: tested, not assumed).
''' </summary>
<Collection("Fixture")>
Public Class I10_LockTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' (a) a second in-process BeginImmediate throws MapLockHeldException in under a second (the SC-008 in-process claim).
    ''' </summary>
    <Fact>
    Public Sub HeldLockIsRefusedInProcessUnderOneSecond()
        Using map As TempMap = New TempMap()
            Using holder As MapDatabase = MapDatabase.OpenOrCreate(map.Path)
                holder.BeginImmediate()
                Dim watch As Stopwatch = Stopwatch.StartNew()
                Using second As MapDatabase = MapDatabase.OpenOrCreate(map.Path)
                    Assert.Throws(Of MapLockHeldException)(Sub() second.BeginImmediate())
                End Using
                watch.Stop()
                Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1), "in-process refusal took " & watch.Elapsed.ToString())
                holder.Rollback()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (b) the executable exits 3 within two seconds including runtime start-up, prints nothing on stdout, and no run row appears after release.
    ''' </summary>
    <Fact>
    Public Sub HeldLockMakesTheExecutableExitThree()
        Using map As TempMap = New TempMap()
            Using holder As MapDatabase = MapDatabase.OpenOrCreate(map.Path)
                holder.BeginImmediate()
                Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path), Nothing, TimeSpan.FromSeconds(30))
                Assert.Equal(3, run.ExitCode)
                Assert.True(run.Elapsed < TimeSpan.FromSeconds(2), "process refusal took " & run.Elapsed.ToString())
                Assert.Equal("", run.StandardOutput)
                Assert.Contains("lock", run.StandardError)
                holder.Rollback()
            End Using
            Assert.Empty(MapQueries.ReadRuns(map.Path))
        End Using
    End Sub

End Class

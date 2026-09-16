' File: ProcessExtractorLauncher.vb
' Project: CodeMem.Bridging
' Description: The production launcher: runs CodeMem.Extractor as a child process with stdout and stderr captured, under the 540 s budget, killed and reported as timed out beyond it (FR-325, FR-327, FR-328; research R47; TIM1).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Threading.Tasks

''' <summary>
''' A .dll runs through dotnet, an .exe directly; the working directory is the bridge's; no window. The bridge holds no map connection while
''' the child runs (the door closes it before calling this).
''' </summary>
Public Class ProcessExtractorLauncher
    Implements IExtractorLauncher

    ''' <summary>
    ''' Runs the extractor to completion or to the budget.
    ''' </summary>
    ''' <param name="request">What to run.</param>
    ''' <returns>The outcome.</returns>
    Public Function Launch(request As LaunchRequest) As LaunchResult Implements IExtractorLauncher.Launch
        Dim arguments As String = "--solution " & Quote(request.SolutionPath) & " --db " & Quote(request.MapPath) & " --solution-key " & Quote(request.SolutionKey)
        Dim psi As ProcessStartInfo
        If request.ExtractorPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) Then
            psi = New ProcessStartInfo("dotnet", Quote(request.ExtractorPath) & " " & arguments)
        Else
            psi = New ProcessStartInfo(request.ExtractorPath, arguments)
        End If
        psi.WorkingDirectory = AppContext.BaseDirectory
        psi.RedirectStandardOutput = True
        psi.RedirectStandardError = True
        psi.StandardOutputEncoding = New UTF8Encoding(False)
        psi.StandardErrorEncoding = New UTF8Encoding(False)
        psi.UseShellExecute = False
        psi.CreateNoWindow = True
        Dim watch As Stopwatch = Stopwatch.StartNew()
        Using process As Process = Process.Start(psi)
            Dim stdoutTask As Task(Of String) = process.StandardOutput.ReadToEndAsync()
            Dim stderrTask As Task(Of String) = process.StandardError.ReadToEndAsync()
            If Not process.WaitForExit(CInt(request.Budget.TotalMilliseconds)) Then
                Try
                    process.Kill(True)
                    process.WaitForExit()
                Catch ex As InvalidOperationException
                    ' already gone
                End Try
                watch.Stop()
                Return New LaunchResult With {.ExitCode = Nothing, .StandardOutput = SafeResult(stdoutTask), .StandardError = SafeResult(stderrTask), .Elapsed = watch.Elapsed, .TimedOut = True}
            End If
            watch.Stop()
            Return New LaunchResult With {.ExitCode = process.ExitCode, .StandardOutput = stdoutTask.Result, .StandardError = stderrTask.Result, .Elapsed = watch.Elapsed, .TimedOut = False}
        End Using
    End Function

    Private Shared Function SafeResult(task As Task(Of String)) As String
        Return If(task.Wait(2000), task.Result, "")
    End Function

    Private Shared Function Quote(value As String) As String
        Return """" & value & """"
    End Function

End Class

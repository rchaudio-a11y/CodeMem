' File: ExtractorProcess.vb
' Project: CodeMem.Tests
' Description: Runs the real CodeMem.Extractor executable and captures its exit code, output and elapsed time (Article XIII).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.Diagnostics
Imports System.IO

''' <summary>
''' Spawns <c>dotnet CodeMem.Extractor.dll</c> from the test output directory, where the project reference places the executable.
''' </summary>
Public Class ExtractorProcess

    ''' <summary>The process exit code.</summary>
    Public ReadOnly Property ExitCode As Integer

    ''' <summary>Captured standard output.</summary>
    Public ReadOnly Property StandardOutput As String

    ''' <summary>Captured standard error.</summary>
    Public ReadOnly Property StandardError As String

    ''' <summary>Wall-clock time of the run.</summary>
    Public ReadOnly Property Elapsed As TimeSpan

    Private Sub New(exitCode As Integer, stdout As String, stderr As String, elapsed As TimeSpan)
        Me.ExitCode = exitCode
        Me.StandardOutput = stdout
        Me.StandardError = stderr
        Me.Elapsed = elapsed
    End Sub

    ''' <summary>
    ''' Runs the extractor with the given arguments and optional environment variables.
    ''' </summary>
    ''' <param name="arguments">Command-line arguments, already quoted where needed.</param>
    ''' <param name="environmentVariables">Extra environment variables, or Nothing.</param>
    ''' <param name="timeout">Maximum wait before the process is killed; default two minutes.</param>
    ''' <returns>The captured result.</returns>
    Public Shared Function Run(arguments As String, Optional environmentVariables As IDictionary(Of String, String) = Nothing, Optional timeout As TimeSpan? = Nothing) As ExtractorProcess
        Dim dll As String = Path.Combine(AppContext.BaseDirectory, "CodeMem.Extractor.dll")
        If Not File.Exists(dll) Then
            Throw New FileNotFoundException("extractor not found beside the tests", dll)
        End If
        Dim psi As ProcessStartInfo = New ProcessStartInfo("dotnet", """" & dll & """ " & arguments) With {
            .WorkingDirectory = AppContext.BaseDirectory,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .UseShellExecute = False,
            .CreateNoWindow = True}
        If environmentVariables IsNot Nothing Then
            For Each pair As KeyValuePair(Of String, String) In environmentVariables
                psi.Environment(pair.Key) = pair.Value
            Next
        End If
        Dim watch As Stopwatch = Stopwatch.StartNew()
        Using process As Process = Process.Start(psi)
            Dim stdoutTask As Threading.Tasks.Task(Of String) = process.StandardOutput.ReadToEndAsync()
            Dim stderrTask As Threading.Tasks.Task(Of String) = process.StandardError.ReadToEndAsync()
            Dim limit As TimeSpan = If(timeout.HasValue, timeout.Value, TimeSpan.FromMinutes(2))
            If Not process.WaitForExit(CInt(limit.TotalMilliseconds)) Then
                process.Kill(True)
                process.WaitForExit()
                Throw New TimeoutException("extractor did not exit within " & limit.ToString() & Environment.NewLine & stderrTask.Result)
            End If
            watch.Stop()
            Return New ExtractorProcess(process.ExitCode, stdoutTask.Result, stderrTask.Result, watch.Elapsed)
        End Using
    End Function

    ''' <summary>
    ''' Wraps a path argument in double quotes.
    ''' </summary>
    ''' <param name="path">The path.</param>
    ''' <returns>The quoted path.</returns>
    Public Shared Function Quote(path As String) As String
        Return """" & path & """"
    End Function

End Class

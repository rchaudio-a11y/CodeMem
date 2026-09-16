' File: ScriptedLauncher.vb
' Project: CodeMem.Tests
' Description: The test launcher: records every request and returns a scripted exit code and lines, a timeout, or waits a delay (feature 004, research R54; SC-306's zero-launch assertions).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Diagnostics
Imports System.Threading
Imports CodeMem.Bridging

''' <summary>
''' The default script is exit 0 with empty output; <see cref="Script"/> and <see cref="ScriptTimeout"/> change it; <see cref="Delay"/>
''' holds the launch so a second call can be attempted while the first is running.
''' </summary>
Public Class ScriptedLauncher
    Implements IExtractorLauncher

    Private _exitCode As Integer? = 0
    Private _stdout As String = ""
    Private _stderr As String = ""
    Private _timedOut As Boolean

    ''' <summary>Every request received, in order.</summary>
    Public ReadOnly Property Requests As List(Of LaunchRequest) = New List(Of LaunchRequest)()

    ''' <summary>How long a launch sleeps before returning; zero by default.</summary>
    Public Property Delay As TimeSpan

    ''' <summary>
    ''' Scripts the next launches' outcome.
    ''' </summary>
    ''' <param name="exitCode">The exit code.</param>
    ''' <param name="stdout">Standard output.</param>
    ''' <param name="stderr">Standard error.</param>
    Public Sub Script(exitCode As Integer, stdout As String, stderr As String)
        _exitCode = exitCode
        _stdout = stdout
        _stderr = stderr
        _timedOut = False
    End Sub

    ''' <summary>
    ''' Scripts a timeout: no exit code, TimedOut true.
    ''' </summary>
    Public Sub ScriptTimeout()
        _exitCode = Nothing
        _stdout = ""
        _stderr = ""
        _timedOut = True
    End Sub

    ''' <summary>
    ''' Records the request, waits the delay, returns the script.
    ''' </summary>
    ''' <param name="request">What the door asked to run.</param>
    ''' <returns>The scripted outcome.</returns>
    Public Function Launch(request As LaunchRequest) As LaunchResult Implements IExtractorLauncher.Launch
        SyncLock Requests
            Requests.Add(request)
        End SyncLock
        Dim watch As Stopwatch = Stopwatch.StartNew()
        If Delay > TimeSpan.Zero Then Thread.Sleep(Delay)
        watch.Stop()
        Return New LaunchResult With {.ExitCode = _exitCode, .StandardOutput = _stdout, .StandardError = _stderr, .Elapsed = watch.Elapsed, .TimedOut = _timedOut}
    End Function

End Class

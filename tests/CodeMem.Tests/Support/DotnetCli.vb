' File: DotnetCli.vb
' Project: CodeMem.Tests
' Description: Runs the platform dotnet CLI for dev-time restore of fixture solutions (Article XV: SDK use is dev-time only).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002): Output added (captures stdout, for comparing the sdk_version stamp with dotnet --version).

Imports System.Diagnostics

''' <summary>
''' Invokes <c>dotnet</c> synchronously and fails loudly on a non-zero exit.
''' </summary>
Public Module DotnetCli

    ''' <summary>
    ''' Runs <c>dotnet</c> with arguments in a directory.
    ''' </summary>
    ''' <param name="arguments">The command line after <c>dotnet</c>.</param>
    ''' <param name="workingDirectory">The working directory.</param>
    Public Sub Run(arguments As String, workingDirectory As String)
        Dim psi As ProcessStartInfo = New ProcessStartInfo("dotnet", arguments) With {
            .WorkingDirectory = workingDirectory,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .UseShellExecute = False,
            .CreateNoWindow = True}
        Using process As Process = Process.Start(psi)
            Dim stdoutTask As Threading.Tasks.Task(Of String) = process.StandardOutput.ReadToEndAsync()
            Dim stderrTask As Threading.Tasks.Task(Of String) = process.StandardError.ReadToEndAsync()
            process.WaitForExit()
            If process.ExitCode <> 0 Then
                Throw New InvalidOperationException("dotnet " & arguments & " failed with exit " & process.ExitCode & Environment.NewLine & stdoutTask.Result & stderrTask.Result)
            End If
        End Using
    End Sub

    ''' <summary>
    ''' Runs <c>dotnet</c> with arguments in a directory and returns its trimmed standard output (fixpack 002: the F04 SDK stamp comparison).
    ''' </summary>
    ''' <param name="arguments">The command line after <c>dotnet</c>.</param>
    ''' <param name="workingDirectory">The working directory.</param>
    ''' <returns>Standard output, trimmed.</returns>
    Public Function Output(arguments As String, workingDirectory As String) As String
        Dim psi As ProcessStartInfo = New ProcessStartInfo("dotnet", arguments) With {
            .WorkingDirectory = workingDirectory,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .UseShellExecute = False,
            .CreateNoWindow = True}
        Using process As Process = Process.Start(psi)
            Dim stdoutTask As Threading.Tasks.Task(Of String) = process.StandardOutput.ReadToEndAsync()
            Dim stderrTask As Threading.Tasks.Task(Of String) = process.StandardError.ReadToEndAsync()
            process.WaitForExit()
            If process.ExitCode <> 0 Then
                Throw New InvalidOperationException("dotnet " & arguments & " failed with exit " & process.ExitCode & Environment.NewLine & stdoutTask.Result & stderrTask.Result)
            End If
            Return stdoutTask.Result.Trim()
        End Using
    End Function


End Module

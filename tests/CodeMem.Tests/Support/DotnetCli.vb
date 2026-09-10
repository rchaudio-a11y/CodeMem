' File: DotnetCli.vb
' Project: CodeMem.Tests
' Description: Runs the platform dotnet CLI for dev-time restore of fixture solutions (Article XV: SDK use is dev-time only).
' Author: RCH Automation LLC
' Created: 2026-09-09

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

End Module

' File: Program.vb
' Project: CodeMem.Extractor
' Description: Console entry point. Parses the command line, runs one extraction, returns the exit code. Nothing else lives here.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Extraction

''' <summary>
''' Entry point of the extractor executable. Wiring only (constitution Article I).
''' </summary>
Public Module Program

    ''' <summary>
    ''' Parses the arguments and runs one extraction through the same method the tests call.
    ''' </summary>
    ''' <param name="args">Command-line arguments per contracts/cli.md.</param>
    ''' <returns>The process exit code.</returns>
    Public Function Main(args As String()) As Integer
        Dim parsed As CommandLine = CommandLine.Parse(args)
        If parsed.ShowHelp Then
            Console.Out.Write(CommandLine.Usage)
            Return CInt(ExitCode.Success)
        End If
        If parsed.ErrorText IsNot Nothing Then
            Console.Error.WriteLine(parsed.ErrorText)
            Console.Error.Write(CommandLine.Usage)
            Return CInt(ExitCode.Failure)
        End If
        Return CInt(ExtractionRun.Execute(parsed.Options, Nothing))
    End Function

End Module

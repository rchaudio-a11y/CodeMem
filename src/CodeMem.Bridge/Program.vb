' File: Program.vb
' Project: CodeMem.Bridge
' Description: Console entry point of the bridge: parses the command line and dispatches to the serve, extract and hook entries. Wiring only (Article I; CON1).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-15 (T043): the extract entry (origin manual, or green-build with --on-green-build; exit 0 accepted, 2 refused) and the hook entry
' (HookEntry.Run over stdin and stdout, always 0) wired to the library's door.

Imports CodeMem.Bridging

''' <summary>
''' Entry point of the bridge executable. Parses the arguments and hands each entry to the library; nothing else lives here.
''' </summary>
Public Module Program

    ''' <summary>
    ''' Parses the arguments and runs the named entry (contracts/cli-config-hook.md §1).
    ''' </summary>
    ''' <param name="args">Command-line arguments.</param>
    ''' <returns>The process exit code.</returns>
    Public Function Main(args As String()) As Integer
        Dim parsed As BridgeCommandLine = BridgeCommandLine.Parse(args)
        If parsed.ShowHelp Then
            Console.Out.Write(BridgeCommandLine.Usage)
            Return 0
        End If
        If parsed.ErrorText IsNot Nothing Then
            Console.Error.WriteLine(parsed.ErrorText)
            Console.Error.Write(BridgeCommandLine.Usage)
            Return 1
        End If
        Select Case parsed.Entry
            Case BridgeCommandLine.ServeEntry
                Try
                    Return BridgeServer.RunAsync(parsed.ConfigPath).GetAwaiter().GetResult()
                Catch ex As Exception
                    Console.Error.WriteLine("serve failed: " & ex.Message)
                    Return 1
                End Try
            Case BridgeCommandLine.ExtractEntry
                Dim door As ExtractDoor = New ExtractDoor(parsed.ConfigPath, New ProcessExtractorLauncher())
                Dim request As ExtractRequest = New ExtractRequest With {
                    .Origin = If(parsed.OnGreenBuild, ExtractOrigin.GreenBuild, ExtractOrigin.Manual),
                    .SolutionKey = parsed.SolutionKey,
                    .RepoPath = parsed.RepoPath,
                    .Stale = parsed.Stale}
                If request.Stale Then
                    Dim staleResult As StaleResult = door.RunStale(request)
                    Console.Out.WriteLine(BridgeJson.Serialize(staleResult))
                    Return If(staleResult.IsFailure, 2, 0)
                End If
                Dim result As ExtractResult = door.Run(request)
                Console.Out.WriteLine(BridgeJson.Serialize(result))
                Return If(result.Refusal IsNot Nothing AndAlso Not result.Launched, 2, 0)
            Case Else
                Return HookEntry.Run(parsed.ConfigPath, Console.In, Console.Out)
        End Select
    End Function

End Module

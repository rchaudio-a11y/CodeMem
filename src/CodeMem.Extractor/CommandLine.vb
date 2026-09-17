' File: CommandLine.vb
' Project: CodeMem.Extractor
' Description: Parses the command line of contracts/cli.md into ExtractionOptions.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-17 (feature 005, T031): the usage names the three inputs (contracts/extractor.md §4).
'
' 2026-09-10 (fixpack 002): usage documents CODEMEM_TEST_NONCE and the both-required rule (FR-117).

Imports CodeMem.Extraction

''' <summary>
''' The parse result: options, a help request, or an error message. Argument names are case-insensitive; values follow as the next argument.
''' </summary>
Public Class CommandLine

    ''' <summary>The parsed options when parsing succeeded; otherwise Nothing.</summary>
    Public Property Options As ExtractionOptions

    ''' <summary>True when --help was given.</summary>
    Public Property ShowHelp As Boolean

    ''' <summary>The usage error, or Nothing.</summary>
    Public Property ErrorText As String

    ''' <summary>
    ''' The usage text. Test-only surfaces are labeled so no unwired surface is presented as real.
    ''' </summary>
    ''' <returns>The usage text.</returns>
    Public Shared ReadOnly Property Usage As String
        Get
            Return "usage: CodeMem.Extractor --solution <path.sln|path.slnx|path.vbproj> --db <path to codemem.sqlite>" & Environment.NewLine &
                "                         [--configuration Debug|Release]      default: Debug" & Environment.NewLine &
                "                         [--framework <tfm>]                   default: the project's first target framework" & Environment.NewLine &
                "                         [--solution-key <name>]               default: solution file name without extension" & Environment.NewLine &
                "       CodeMem.Extractor --help" & Environment.NewLine &
                Environment.NewLine &
                "exit codes: 0 completed; 1 usage, load, schema or database failure; 2 compile errors; 3 lock held; 4 residual mismatch" & Environment.NewLine &
                Environment.NewLine &
                "inputs: a .sln, a .slnx (opened by CodeMem's own parse; folders and configurations are not read) or a .vbproj" & Environment.NewLine &
                Environment.NewLine &
                "test-only environment variables:" & Environment.NewLine &
                "  CODEMEM_TEST_ABORT_AT=<phase>:<nonce>   phase: DuringInitialize, DuringUpgrade, AfterStaging or DuringPublish (invariants I9, F6, upgrade)" & Environment.NewLine &
                "  CODEMEM_TEST_NONCE=<nonce>              the per-run nonce a test mints" & Environment.NewLine &
                "  both are required and the nonces must be equal; either alone, or unequal nonces, is inert and the run completes normally" & Environment.NewLine
        End Get
    End Property

    ''' <summary>
    ''' Parses the arguments.
    ''' </summary>
    ''' <param name="args">The raw arguments.</param>
    ''' <returns>The result.</returns>
    Public Shared Function Parse(args As String()) As CommandLine
        Dim result As CommandLine = New CommandLine()
        Dim options As ExtractionOptions = New ExtractionOptions()
        Dim i As Integer = 0
        While i < args.Length
            Dim name As String = args(i).ToLowerInvariant()
            If name = "--help" OrElse name = "-h" OrElse name = "/?" Then
                result.ShowHelp = True
                Return result
            End If
            If Not name.StartsWith("--", StringComparison.Ordinal) Then
                result.ErrorText = "unexpected argument: " & args(i)
                Return result
            End If
            If i + 1 >= args.Length Then
                result.ErrorText = "missing value for " & args(i)
                Return result
            End If
            Dim value As String = args(i + 1)
            Select Case name
                Case "--solution" : options.SolutionPath = value
                Case "--db" : options.DbPath = value
                Case "--configuration" : options.Configuration = value
                Case "--framework" : options.Framework = value
                Case "--solution-key" : options.SolutionKey = value
                Case Else
                    result.ErrorText = "unknown argument: " & args(i)
                    Return result
            End Select
            i += 2
        End While
        If String.IsNullOrEmpty(options.SolutionPath) Then
            result.ErrorText = "--solution is required"
            Return result
        End If
        If String.IsNullOrEmpty(options.DbPath) Then
            result.ErrorText = "--db is required"
            Return result
        End If
        result.Options = options
        Return result
    End Function

End Class

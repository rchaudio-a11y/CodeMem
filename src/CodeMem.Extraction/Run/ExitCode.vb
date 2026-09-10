' File: ExitCode.vb
' Project: CodeMem.Extraction
' Description: The process exit codes of contracts/cli.md (FR-034).
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' Exit codes. No failure exits 0.
''' </summary>
Public Enum ExitCode
    ''' <summary>Completed and published.</summary>
    Success = 0
    ''' <summary>Usage error, unreadable solution, workspace failure, schema mismatch, duplicate doc id, database error.</summary>
    Failure = 1
    ''' <summary>The compilation has Error diagnostics; nothing written.</summary>
    BuildErrors = 2
    ''' <summary>The map lock is held by another extractor or unobtainable.</summary>
    LockHeld = 3
    ''' <summary>A residual is non-zero; one failed extract_runs row written.</summary>
    ResidualMismatch = 4
End Enum

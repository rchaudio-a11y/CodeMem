' File: ExtractOrigin.vb
' Project: CodeMem.Bridging
' Description: The three doors an extract can come through (FR-331, spec Q5, research R48); only green-build meets the second gate.
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' tool (the MCP surface), green_build (the hook) or manual (a human at the command line).
''' </summary>
Public Enum ExtractOrigin
    ''' <summary>A call on the MCP surface.</summary>
    Tool
    ''' <summary>The PostToolUse hook, or --on-green-build.</summary>
    GreenBuild
    ''' <summary>A human at the command line.</summary>
    Manual
End Enum

' File: ResolvedTarget.vb
' Project: CodeMem.Bridging
' Description: What target resolution hands the launch: the registry's key, the map's last-seen path, the configured map (FR-327; data-model §8).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Produced by <see cref="TargetResolver"/> from a key or a path; never defaulted.
''' </summary>
Public Class ResolvedTarget

    ''' <summary>The registry's solution_key, passed as --solution-key.</summary>
    Public Property SolutionKey As String

    ''' <summary>The map's last_seen_path, passed as --solution.</summary>
    Public Property SolutionPath As String

    ''' <summary>The configured map, passed as --db.</summary>
    Public Property MapPath As String

    ''' <summary>The map's solution id.</summary>
    Public Property SolutionId As Long

End Class

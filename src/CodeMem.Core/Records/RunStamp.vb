' File: RunStamp.vb
' Project: CodeMem.Core
' Description: The provenance fields of an extract_runs row (constitution Article V).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002): SdkVersion added (schema version 2).

''' <summary>
''' The stamp every run carries: what was compiled, from which commit, by which extractor and schema.
''' </summary>
Public Class RunStamp

    ''' <summary>Owning solution.</summary>
    Public Property SolutionId As Long

    ''' <summary>SHA-256 hex over every compiled input (FR-005). Always present.</summary>
    Public Property SourceDigest As String

    ''' <summary>HEAD commit sha, or Nothing when no repository.</summary>
    Public Property CommitSha As String

    ''' <summary>True when any compiled input differs from HEAD; Nothing when no repository.</summary>
    Public Property IsDirty As Boolean?

    ''' <summary>The build configuration compiled.</summary>
    Public Property BuildConfiguration As String

    ''' <summary>The target framework compiled.</summary>
    Public Property TargetFramework As String

    ''' <summary>Extractor version, Major.Minor.Build.</summary>
    Public Property ExtractorVersion As String

    ''' <summary>Schema version written.</summary>
    Public Property SchemaVersion As Integer

    ''' <summary>The resolved .NET SDK version for the solution directory (research R21), e.g. 10.0.401; never Nothing on a version-2 run (FR-109).</summary>
    Public Property SdkVersion As String

    ''' <summary>ISO-8601 UTC start of the run.</summary>
    Public Property StartedUtc As String

    ''' <summary>ISO-8601 UTC end of the run.</summary>
    Public Property FinishedUtc As String

End Class

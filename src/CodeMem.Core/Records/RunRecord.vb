' File: RunRecord.vb
' Project: CodeMem.Core
' Description: One extract_runs row as read from the map: the stamp, the outcome and the ten counts (feature 004; 056 §1.1 latestRun).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Every column of an <c>extract_runs</c> row, nullable where the schema is.
''' </summary>
Public Class RunRecord

    ''' <summary>The run id.</summary>
    Public Property Id As Long

    ''' <summary>Owning solution.</summary>
    Public Property SolutionId As Long

    ''' <summary>completed or failed.</summary>
    Public Property Outcome As String

    ''' <summary>The source digest.</summary>
    Public Property SourceDigest As String

    ''' <summary>HEAD commit sha, or Nothing when no repository.</summary>
    Public Property CommitSha As String

    ''' <summary>The dirty flag, or Nothing exactly when CommitSha is Nothing.</summary>
    Public Property IsDirty As Boolean?

    ''' <summary>The build configuration compiled.</summary>
    Public Property BuildConfiguration As String

    ''' <summary>The target framework compiled.</summary>
    Public Property TargetFramework As String

    ''' <summary>Extractor version.</summary>
    Public Property ExtractorVersion As String

    ''' <summary>Schema version written.</summary>
    Public Property SchemaVersion As Integer

    ''' <summary>The SDK version, or Nothing on a version-1 row.</summary>
    Public Property SdkVersion As String

    ''' <summary>ISO-8601 UTC start.</summary>
    Public Property StartedUtc As String

    ''' <summary>ISO-8601 UTC finish.</summary>
    Public Property FinishedUtc As String

    ''' <summary>The ten counts.</summary>
    Public Property Counts As RunCounts

End Class

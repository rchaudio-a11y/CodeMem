' File: GitFacts.vb
' Project: CodeMem.Extraction
' Description: What the repository says about a run: commit sha, dirty flag and working directory; all null without a repository.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' Git provenance of one run (FR-006). CommitSha and IsDirty are both null or both non-null.
''' </summary>
Public Class GitFacts

    ''' <summary>HEAD commit sha, or Nothing.</summary>
    Public Property CommitSha As String

    ''' <summary>True when any compiled input is modified, staged or untracked; Nothing without a repository.</summary>
    Public Property IsDirty As Boolean?

    ''' <summary>The repository working directory, or Nothing.</summary>
    Public Property RepoRoot As String

End Class

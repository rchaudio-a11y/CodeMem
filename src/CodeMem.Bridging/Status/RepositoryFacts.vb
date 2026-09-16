' File: RepositoryFacts.vb
' Project: CodeMem.Bridging
' Description: The repository's own answers for one bound solution, through LibGit2Sharp: discovery, HEAD, the tree-wide dirty check, whether the recorded commit is an ancestor and how far behind (FR-320, FR-322; research R46).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Extraction
Imports LibGit2Sharp

''' <summary>
''' The one place LibGit2Sharp is called in the bridge. Nothing is guessed: every fact that could not be read is Nothing with a note.
''' </summary>
Public Class RepositoryFacts

    ''' <summary>True when the root is a repository working directory with a commit.</summary>
    Public Property Available As Boolean

    ''' <summary>Why a fact could not be read, or Nothing.</summary>
    Public Property Note As String

    ''' <summary>HEAD's sha, or Nothing.</summary>
    Public Property HeadSha As String

    ''' <summary>Whether the whole working tree has uncommitted or untracked, non-ignored changes; Nothing when unavailable.</summary>
    Public Property TreeDirty As Boolean?

    ''' <summary>True when the recorded commit exists in the repository.</summary>
    Public Property RecordedFound As Boolean

    ''' <summary>True when the recorded commit is HEAD or an ancestor of HEAD.</summary>
    Public Property IsAncestor As Boolean

    ''' <summary>Commits from the recorded one to HEAD when it is an ancestor; Nothing otherwise.</summary>
    Public Property BehindBy As Integer?

    ''' <summary>
    ''' Reads the facts at a registered root against a recorded commit.
    ''' </summary>
    ''' <param name="repoRoot">The map's repo_root for the solution.</param>
    ''' <param name="recordedSha">The commit the latest completed run recorded.</param>
    ''' <returns>The facts.</returns>
    Public Shared Function Read(repoRoot As String, recordedSha As String) As RepositoryFacts
        Dim facts As RepositoryFacts = New RepositoryFacts()
        Try
            Dim discovered As String = Repository.Discover(repoRoot)
            If String.IsNullOrEmpty(discovered) Then
                facts.Note = "no repository at " & repoRoot
                Return facts
            End If
            Using repo As Repository = New Repository(discovered)
                Dim workingDirectory As String = repo.Info.WorkingDirectory
                If Not SameDirectory(workingDirectory, repoRoot) Then
                    facts.Note = "root is not a repository working directory: found " & workingDirectory
                    Return facts
                End If
                Dim tip As Commit = repo.Head.Tip
                If tip Is Nothing Then
                    facts.Note = "no commit"
                    Return facts
                End If
                facts.HeadSha = tip.Sha
                facts.TreeDirty = repo.RetrieveStatus(StatusScan()).IsDirty
                Dim recorded As Commit = repo.Lookup(Of Commit)(recordedSha)
                If recorded Is Nothing Then
                    facts.Note = "recorded commit not in this repository"
                Else
                    facts.RecordedFound = True
                    If String.Equals(recorded.Sha, tip.Sha, StringComparison.Ordinal) Then
                        facts.IsAncestor = True
                        facts.BehindBy = 0
                    Else
                        Dim mergeBase As Commit = repo.ObjectDatabase.FindMergeBase(recorded, tip)
                        facts.IsAncestor = mergeBase IsNot Nothing AndAlso String.Equals(mergeBase.Sha, recorded.Sha, StringComparison.Ordinal)
                        If facts.IsAncestor Then
                            Dim filter As CommitFilter = New CommitFilter With {.IncludeReachableFrom = tip, .ExcludeReachableFrom = recorded}
                            Dim count As Integer = 0
                            For Each unused As Commit In repo.Commits.QueryBy(filter)
                                count += 1
                            Next
                            facts.BehindBy = count
                        End If
                    End If
                End If
                facts.Available = True
                Return facts
            End Using
        Catch ex As LibGit2SharpException
            facts.Available = False
            facts.Note = ex.Message
            Return facts
        End Try
    End Function

    ''' <summary>
    ''' The status scan tuned for one question, is the tree dirty: ignored files are not enumerated (bin/ and obj/ trees are the bulk of a
    ''' checkout), renames are not detected, untracked directories are reported once, submodules excluded. IsDirty is unaffected: it is any
    ''' entry that is neither unaltered nor ignored (T035: the live scan of three trees read 3.07 s with the defaults, over SC-301's 3 s).
    ''' </summary>
    ''' <returns>The options.</returns>
    Private Shared Function StatusScan() As StatusOptions
        Return New StatusOptions With {
            .IncludeIgnored = False,
            .IncludeUnaltered = False,
            .DetectRenamesInIndex = False,
            .DetectRenamesInWorkDir = False,
            .RecurseUntrackedDirs = False,
            .ExcludeSubmodules = True}
    End Function

    Private Shared Function SameDirectory(a As String, b As String) As Boolean
        Return String.Equals(SolutionScope.Resolve(a, a).Root, SolutionScope.Resolve(b, b).Root, StringComparison.OrdinalIgnoreCase)
    End Function

End Class

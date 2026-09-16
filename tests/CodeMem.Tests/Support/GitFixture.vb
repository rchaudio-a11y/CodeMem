' File: GitFixture.vb
' Project: CodeMem.Tests
' Description: Shared helpers over LibGit2Sharp for the repository states map_status is tested against (feature 004, research R39, R54).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO
Imports LibGit2Sharp

''' <summary>
''' Creates a repository around a fixture copy, commits, touches files, branches, and removes the .git directory again. LibGit2Sharp
''' resolves transitively through CodeMem.Extraction (R01 already imports it).
''' </summary>
Public Module GitFixture

    ''' <summary>
    ''' Initialises a repository at a directory and writes a .gitignore for bin/ and obj/.
    ''' </summary>
    ''' <param name="directory">The working directory; created if absent.</param>
    ''' <returns>The open repository; the caller disposes it.</returns>
    Public Function Init(directory As String) As Repository
        IO.Directory.CreateDirectory(directory)
        File.WriteAllText(Path.Combine(directory, ".gitignore"), "bin/" & vbLf & "obj/" & vbLf)
        Return New Repository(Repository.Init(directory))
    End Function

    ''' <summary>
    ''' Stages everything and commits with a fixed signature.
    ''' </summary>
    ''' <param name="repo">The repository.</param>
    ''' <param name="message">The commit message.</param>
    ''' <returns>The new commit's sha.</returns>
    Public Function CommitAll(repo As Repository, message As String) As String
        Commands.Stage(repo, "*")
        Dim who As Signature = New Signature("CodeMem.Tests", "tests@codemem.invalid", DateTimeOffset.Now)
        Return repo.Commit(message, who, who).Sha
    End Function

    ''' <summary>
    ''' Writes a file under the directory (creating its folder), so the tree changes.
    ''' </summary>
    ''' <param name="directory">The working directory.</param>
    ''' <param name="relativeFile">Path relative to the directory, forward slashes.</param>
    ''' <param name="text">The file's text.</param>
    Public Sub Touch(directory As String, relativeFile As String, text As String)
        Dim full As String = Path.Combine(directory, relativeFile.Replace("/"c, Path.DirectorySeparatorChar))
        IO.Directory.CreateDirectory(Path.GetDirectoryName(full))
        File.WriteAllText(full, text)
    End Sub

    ''' <summary>
    ''' Creates a branch and checks it out, discarding uncommitted changes to tracked files.
    ''' </summary>
    ''' <param name="repo">The repository.</param>
    ''' <param name="name">The branch name.</param>
    ''' <param name="fromSha">The commit the branch starts at, or Nothing for HEAD.</param>
    Public Sub CheckoutNewBranch(repo As Repository, name As String, Optional fromSha As String = Nothing)
        Dim branch As Branch = If(fromSha Is Nothing, repo.CreateBranch(name), repo.CreateBranch(name, repo.Lookup(Of Commit)(fromSha)))
        Commands.Checkout(repo, branch, New CheckoutOptions With {.CheckoutModifiers = CheckoutModifiers.Force})
    End Sub

    ''' <summary>
    ''' Restores the working tree to HEAD, discarding modifications to tracked files.
    ''' </summary>
    ''' <param name="repo">The repository.</param>
    Public Sub DiscardChanges(repo As Repository)
        repo.Reset(ResetMode.Hard)
    End Sub

    ''' <summary>
    ''' Deletes the .git directory (clearing read-only pack files first) so the working tree is no longer a repository. Dispose the
    ''' repository before calling this.
    ''' </summary>
    ''' <param name="directory">The working directory.</param>
    Public Sub RemoveGitDirectory(directory As String)
        Dim git As String = Path.Combine(directory, ".git")
        If Not IO.Directory.Exists(git) Then Return
        For Each file As String In IO.Directory.GetFiles(git, "*", SearchOption.AllDirectories)
            IO.File.SetAttributes(file, FileAttributes.Normal)
        Next
        IO.Directory.Delete(git, True)
    End Sub

End Module

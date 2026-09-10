' File: GitProvenance.vb
' Project: CodeMem.Extraction
' Description: Reads commit sha and dirty flag in-process through LibGit2Sharp over the compiled inputs (FR-006, research R8).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO
Imports LibGit2Sharp

''' <summary>
''' Never launches a process. The dirty flag is evaluated over exactly the compiled-inputs enumeration (one door).
''' </summary>
Public Module GitProvenance

    ''' <summary>
    ''' Discovers the repository containing the base directory and reads HEAD and the status of every input.
    ''' </summary>
    ''' <param name="baseDirectory">The solution's base directory.</param>
    ''' <param name="inputs">The compiled inputs.</param>
    ''' <returns>The facts; all null when no repository or no commit exists.</returns>
    Public Function Read(baseDirectory As String, inputs As IEnumerable(Of CompiledInput)) As GitFacts
        Dim facts As GitFacts = New GitFacts()
        Dim discovered As String = Repository.Discover(baseDirectory)
        If String.IsNullOrEmpty(discovered) Then Return facts
        Using repo As Repository = New Repository(discovered)
            Dim tip As Commit = repo.Head.Tip
            If tip Is Nothing Then Return facts
            Dim workingDirectory As String = repo.Info.WorkingDirectory
            Dim dirty As Boolean = False
            For Each input As CompiledInput In inputs
                Dim relative As String = Path.GetRelativePath(workingDirectory, input.FullPath).Replace("\"c, "/"c)
                If relative.StartsWith("../", StringComparison.Ordinal) Then Continue For
                Dim status As FileStatus = repo.RetrieveStatus(relative)
                If status <> FileStatus.Unaltered AndAlso (status And FileStatus.Ignored) <> FileStatus.Ignored Then
                    dirty = True
                    Exit For
                End If
            Next
            facts.CommitSha = tip.Sha
            facts.IsDirty = dirty
            facts.RepoRoot = workingDirectory
        End Using
        Return facts
    End Function

End Module

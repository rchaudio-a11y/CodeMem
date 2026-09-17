' File: WarningsCopy.vb
' Project: CodeMem.Tests
' Description: A temp copy of one warnings fixture project (its top-level files) beside a NuGet.config whose relative sources are made absolute, so a fact can edit the project and restore it outside the tree (feature 005, X02).
' Author: RCH Automation LLC
' Created: 2026-09-17

Imports System.IO
Imports System.Text.RegularExpressions

''' <summary>
''' bin/ and obj/ are not copied: the fact restores the copy itself when it needs an assets file.
''' </summary>
Public Class WarningsCopy
    Implements IDisposable

    ''' <summary>The copy's root: the NuGet.config and the project directory live under it.</summary>
    Public ReadOnly Property Root As String

    ''' <summary>The copied project's directory.</summary>
    Public ReadOnly Property Directory As String

    ''' <summary>The copied project file.</summary>
    Public ReadOnly Property ProjectPath As String

    ''' <summary>
    ''' Copies the project's top-level files and writes the NuGet.config.
    ''' </summary>
    ''' <param name="sourceProjectPath">A project under Fixtures/Warnings.</param>
    Public Sub New(sourceProjectPath As String)
        Dim sourceDirectory As String = Path.GetDirectoryName(sourceProjectPath)
        Root = Path.Combine(Path.GetTempPath(), "codemem-tests", "warnings-" & Guid.NewGuid().ToString("N"))
        Directory = Path.Combine(Root, Path.GetFileName(sourceDirectory))
        IO.Directory.CreateDirectory(Directory)
        For Each file As String In IO.Directory.GetFiles(sourceDirectory)
            IO.File.Copy(file, Path.Combine(Directory, Path.GetFileName(file)))
        Next
        Dim configDirectory As String = Path.GetDirectoryName(sourceDirectory)
        Dim config As String = IO.File.ReadAllText(Path.Combine(configDirectory, "NuGet.config"))
        config = Regex.Replace(config, "value=""([^""]+)""",
                               Function(match As Match)
                                   Dim value As String = match.Groups(1).Value
                                   If Path.IsPathRooted(value) OrElse value.Contains("://", StringComparison.Ordinal) Then Return match.Value
                                   Dim candidate As String = Path.GetFullPath(Path.Combine(configDirectory, value))
                                   If IO.Directory.Exists(candidate) OrElse IO.File.Exists(candidate) Then Return "value=""" & candidate & """"
                                   Return match.Value
                               End Function)
        IO.File.WriteAllText(Path.Combine(Root, "NuGet.config"), config)
        ProjectPath = Path.Combine(Directory, Path.GetFileName(sourceProjectPath))
    End Sub

    ''' <summary>
    ''' Restores the copy; the exit code is returned, never thrown (a restore that fails by design still writes its assets file).
    ''' </summary>
    ''' <returns>dotnet restore's exit code.</returns>
    Public Function Restore() As Integer
        Return DotnetCli.TryRun("restore """ & ProjectPath & """", Directory)
    End Function

    ''' <summary>
    ''' Deletes the copy.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            IO.Directory.Delete(Root, True)
        Catch ex As IOException
        Catch ex As UnauthorizedAccessException
        End Try
    End Sub

End Class

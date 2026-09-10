' File: FileHeaderGateTests.vb
' Project: CodeMem.Tests
' Description: Review gate - every .vb file starts with the header block and every public declaration carries an XML summary.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' A public declaration may also carry <inheritdoc/> (the edge rules document Collect once on the interface).
' GREEN: 2026-09-09 first run.
' FIRE:  2026-09-09 deleted the five header lines of SchemaVersion.vb -> red (five findings); reverted -> green.

Imports System.IO
Imports System.Text.RegularExpressions
Imports Xunit

''' <summary>
''' The constitution's header and XML-documentation rules, made mechanical because the VB compiler does not enforce them.
''' </summary>
Public Class FileHeaderGateTests

    Private Shared ReadOnly Declaration As Regex = New Regex("^\s*Public\s+(?:(?:Shared|Overridable|Overrides|MustOverride|NotOverridable|ReadOnly|WriteOnly|Partial|MustInherit|NotInheritable|Async|Iterator|Overloads|Shadows|Const)\s+)*(Class|Module|Interface|Enum|Structure|Delegate|Sub|Function|Property|Event)\b", RegexOptions.Compiled)

    ''' <summary>
    ''' Header block: own file name, project name, Description, Author, Created date.
    ''' </summary>
    <Fact>
    Public Sub EveryFileStartsWithTheHeaderBlock()
        Dim failures As List(Of String) = New List(Of String)()
        For Each file As String In AllSourceFiles()
            Dim lines As String() = IO.File.ReadAllLines(file)
            Dim header As List(Of String) = New List(Of String)()
            For Each line As String In lines
                If Not line.StartsWith("'", StringComparison.Ordinal) Then Exit For
                header.Add(line)
            Next
            Dim text As String = String.Join(vbLf, header)
            Dim name As String = Path.GetFileName(file)
            If header.Count = 0 OrElse Not header(0).StartsWith("' File: " & name, StringComparison.Ordinal) Then failures.Add(name & ": missing or wrong File line")
            If Not text.Contains("' Project: " & ProjectNameOf(file)) Then failures.Add(name & ": missing or wrong Project line")
            If Not text.Contains("' Description:") Then failures.Add(name & ": missing Description")
            If Not text.Contains("' Author: RCH Automation LLC") Then failures.Add(name & ": missing Author")
            If Not Regex.IsMatch(text, "' Created: \d{4}-\d{2}-\d{2}") Then failures.Add(name & ": missing Created date")
        Next
        Assert.True(failures.Count = 0, String.Join(Environment.NewLine, failures))
    End Sub

    ''' <summary>
    ''' Every Public Class/Module/Interface/Enum/Structure/Delegate/Sub/Function/Property/Event is immediately preceded by a documentation block (attributes may sit between).
    ''' </summary>
    <Fact>
    Public Sub EveryPublicDeclarationHasXmlDocumentation()
        Dim failures As List(Of String) = New List(Of String)()
        For Each file As String In AllSourceFiles()
            Dim lines As String() = IO.File.ReadAllLines(file)
            For i As Integer = 0 To lines.Length - 1
                If Not Declaration.IsMatch(lines(i)) Then Continue For
                Dim j As Integer = i - 1
                While j >= 0 AndAlso Regex.IsMatch(lines(j), "^\s*<.*>\s*$")
                    j -= 1
                End While
                Dim documented As Boolean = False
                Dim sawDocLine As Boolean = False
                While j >= 0 AndAlso lines(j).TrimStart().StartsWith("'''", StringComparison.Ordinal)
                    sawDocLine = True
                    If lines(j).Contains("<summary>") OrElse lines(j).Contains("<inheritdoc") Then documented = True
                    j -= 1
                End While
                If Not (sawDocLine AndAlso documented) Then failures.Add(Path.GetFileName(file) & "(" & (i + 1) & "): " & lines(i).Trim())
            Next
        Next
        Assert.True(failures.Count = 0, String.Join(Environment.NewLine, failures))
    End Sub

    Private Shared Function AllSourceFiles() As List(Of String)
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim files As List(Of String) = SqlLocationGateTests.SourceFiles(Path.Combine(root, "src"))
        files.AddRange(SqlLocationGateTests.SourceFiles(Path.Combine(root, "tests")))
        Return files
    End Function

    Private Shared Function ProjectNameOf(file As String) As String
        Dim dir As DirectoryInfo = New DirectoryInfo(Path.GetDirectoryName(file))
        While dir IsNot Nothing
            Dim projects As FileInfo() = dir.GetFiles("*.vbproj")
            If projects.Length > 0 Then Return Path.GetFileNameWithoutExtension(projects(0).Name)
            dir = dir.Parent
        End While
        Throw New FileNotFoundException("no .vbproj above " & file)
    End Function

End Class

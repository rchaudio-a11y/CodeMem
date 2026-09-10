' File: TripwireTests.vb
' Project: CodeMem.Tests
' Description: I13 - the repositories contain write statements and no destructive form reaching code_symbols (FR-038, SC-012).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' Vacuous-Red rule (Article II): the positive count of INSERT/UPDATE literals is asserted first, so an empty or
' unparsed repository folder cannot pass the absence assertions by accident.
' GREEN: 2026-09-09 first run.
' FIRE:  2026-09-09 added a Purge method with "DELETE FROM code_symbols WHERE id = @id" to CodeSymbolsRepository -> red; reverted -> green.

Imports System.IO
Imports System.Text.RegularExpressions
Imports Xunit

''' <summary>
''' Scans the string literals of every .vb file under Core/Repositories and Core/Schema.
''' </summary>
Public Class TripwireTests

    Private Shared ReadOnly StringLiteral As Regex = New Regex("""(?:[^""]|"""")*""", RegexOptions.Compiled)

    ''' <summary>
    ''' Positive writes first, then each destructive form asserted absent separately.
    ''' </summary>
    <Fact>
    Public Sub NoPathDeletesDropsRecreatesOrCascadesIntoCodeSymbols()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim literalsByFile As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each folder As String In New String() {Path.Combine(root, "src", "CodeMem.Core", "Repositories"), Path.Combine(root, "src", "CodeMem.Core", "Schema")}
            For Each file As String In Directory.GetFiles(folder, "*.vb")
                Dim literals As List(Of String) = New List(Of String)()
                For Each match As Match In StringLiteral.Matches(IO.File.ReadAllText(file))
                    literals.Add(match.Value)
                Next
                literalsByFile(file) = String.Join(vbLf, literals)
            Next
        Next
        Dim everything As String = String.Join(vbLf, literalsByFile.Values)

        Dim writes As Integer = Regex.Matches(everything, "INSERT INTO").Count + Regex.Matches(everything, "UPDATE ").Count
        Assert.True(writes > 0, "no INSERT/UPDATE statements found: the scan is vacuous")

        Assert.Equal(0, Regex.Matches(everything, "DELETE FROM code_symbols").Count)
        Assert.Equal(0, Regex.Matches(everything, "DROP TABLE code_symbols").Count)
        Assert.Equal(0, Regex.Matches(everything, "DROP TABLE IF EXISTS code_symbols").Count)
        For Each pair As KeyValuePair(Of String, String) In literalsByFile
            Dim create As Integer = pair.Value.IndexOf("CREATE TABLE code_symbols", StringComparison.Ordinal)
            If create >= 0 Then
                Assert.True(pair.Value.IndexOf("DROP", 0, create, StringComparison.Ordinal) < 0, Path.GetFileName(pair.Key) & ": CREATE TABLE code_symbols is preceded by a DROP")
            End If
        Next
        Assert.Equal(0, Regex.Matches(everything, "ON DELETE CASCADE").Count)
    End Sub

End Class

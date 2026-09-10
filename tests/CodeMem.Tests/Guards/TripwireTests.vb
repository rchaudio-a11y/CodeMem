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
'
' 2026-09-10 (fixpack 002, F10 / FR-121): every literal is whitespace-normalised (\s+ -> one space) and every pattern, the positive
' INSERT INTO / UPDATE count included, matches with RegexOptions.IgnoreCase (research R29). The 2026-09-09 fire was canonical spelling;
' the re-fire below is lowercase with three spaces.
' FIRE:  2026-09-10 (T044) added a FireProbe method with "delete   from code_symbols where id = @id" (lowercase, three spaces) to
'        CodeSymbolsRepository -> red (DELETE FROM code_symbols count expected 0, actual 1); reverted -> green.

Imports System.IO
Imports System.Text.RegularExpressions
Imports Xunit

''' <summary>
''' Scans the string literals of every .vb file under Core/Repositories and Core/Schema, case-insensitively with whitespace normalised.
''' </summary>
Public Class TripwireTests

    Private Shared ReadOnly StringLiteral As Regex = New Regex("""(?:[^""]|"""")*""", RegexOptions.Compiled)
    Private Shared ReadOnly Whitespace As Regex = New Regex("\s+", RegexOptions.Compiled)

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
                    literals.Add(Normalise(match.Value))
                Next
                literalsByFile(file) = String.Join(vbLf, literals)
            Next
        Next
        Dim everything As String = String.Join(vbLf, literalsByFile.Values)

        Dim writes As Integer = Count(everything, "INSERT INTO") + Count(everything, "UPDATE ")
        Assert.True(writes > 0, "no INSERT/UPDATE statements found: the scan is vacuous")

        Assert.Equal(0, Count(everything, "DELETE FROM code_symbols"))
        Assert.Equal(0, Count(everything, "DROP TABLE code_symbols"))
        Assert.Equal(0, Count(everything, "DROP TABLE IF EXISTS code_symbols"))
        For Each pair As KeyValuePair(Of String, String) In literalsByFile
            Dim create As Integer = pair.Value.IndexOf("CREATE TABLE code_symbols", StringComparison.OrdinalIgnoreCase)
            If create >= 0 Then
                Assert.True(pair.Value.IndexOf("DROP", 0, create, StringComparison.OrdinalIgnoreCase) < 0, Path.GetFileName(pair.Key) & ": CREATE TABLE code_symbols is preceded by a DROP")
            End If
        Next
        Assert.Equal(0, Count(everything, "ON DELETE CASCADE"))
    End Sub

    ''' <summary>
    ''' Collapses every run of whitespace to one space so "delete   from" and "delete\tfrom" read as "delete from" (FR-121).
    ''' </summary>
    ''' <param name="text">A string literal.</param>
    ''' <returns>The normalised text.</returns>
    Public Shared Function Normalise(text As String) As String
        Return Whitespace.Replace(text, " ")
    End Function

    Private Shared Function Count(text As String, pattern As String) As Integer
        Return Regex.Matches(text, Regex.Escape(pattern), RegexOptions.IgnoreCase).Count
    End Function

End Class

' File: BridgeSqlGateTests.vb
' Project: CodeMem.Tests
' Description: Review gate (feature 004, FR-303, FR-304): no SQL literal in either bridge project; Core's read methods are SELECT-only; the registry module names code_map_solutions and nothing else; the store is opened only by the bridge.
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' The keyword scans are case-sensitive over string literals (analyze pass 2, U4): the bridge's tool descriptions and refusal texts are
' prose that says "delete" or "created" in lowercase, and a SQL keyword in this repository is written in capitals; SqlLocationGateTests
' still scans the whole tree case-insensitively, so lowercase SQL is caught there. Excluded from SqlLocationGateTests: the literals here
' name the SQL they search for.
' RED:   2026-09-15 (T008) fact (2) red for the stated reason - "no SELECT literal under Core/Repositories/Registry: the scan is vacuous"
'        (no Registry/ folder yet); (1), (3), (4) green - (1) and (4) are guards on absence, trusted through the T013 fires; (3) found the
'        existing Read functions of Core (ReadActive, ReadRetiredByDocIds, ReadByKey, ReadGuid) SELECT-only.
' 2026-09-15 (T013): two refinements once the subjects existed - whole-line comments are dropped before every scan (XML docs quote member
'        names: ReadOnlyConnectionString was charged with the next method's <see cref>), and (4) treats Core/Repositories/Registry/ and
'        StoreDatabase.vb as the definitions (the registry module takes a StoreDatabase; nothing else in Core, Extraction or the Extractor may).
' GREEN: 2026-09-15 (T013) 4 of 4 once CodeMapSolutionsRepository landed (2 finds its SELECT).
' FIRE:  2026-09-15 (T013) (2): a "SELECT id FROM projects" literal in CodeMapSolutionsRepository.vb -> red (expected code_map_solutions,
'        actual projects); (1): a "SELECT 1" literal in BridgeCommandLine.vb -> red (BridgeCommandLine.vb: SELECT 1); (4): Dim probe As
'        StoreDatabase in src/CodeMem.Extraction/Run/ExtractionRun.vb -> red (ExtractionRun.vb: StoreDatabase); each reverted -> green.

Imports System.IO
Imports System.Text.RegularExpressions
Imports Xunit

''' <summary>
''' Four facts over string literals: (1) none with a SQL keyword under src/CodeMem.Bridge or src/CodeMem.Bridging; (2) the registry module's
''' literals are SELECTs naming code_map_solutions only, never sqlite_master (INC1); (3) every Read/Search function in Core's repositories
''' begins with SELECT or WITH and writes nothing; (4) StoreDatabase and CodeMapSolutionsRepository are referenced only by the bridge and
''' the tests (v1.3.0's last sentence: the extractor never opens the store).
''' </summary>
Public Class BridgeSqlGateTests

    Private Shared ReadOnly StringLiteral As Regex = New Regex("""(?:[^""]|"""")*""", RegexOptions.Compiled)
    Private Shared ReadOnly SqlKeyword As Regex = New Regex("\b(SELECT|INSERT|UPDATE|DELETE|CREATE|DROP|ALTER|ATTACH|PRAGMA)\b", RegexOptions.Compiled)
    Private Shared ReadOnly WriteKeyword As Regex = New Regex("\b(INSERT|UPDATE|DELETE|CREATE|DROP|ALTER|ATTACH|PRAGMA)\b", RegexOptions.Compiled)
    Private Shared ReadOnly Declaration As Regex = New Regex("^[ \t]*(?:Public|Private|Friend|Protected)?[ \t]*(?:Shared[ \t]+)?(?:Function|Sub)[ \t]+(\w+)", RegexOptions.Compiled Or RegexOptions.Multiline)
    Private Shared ReadOnly TableAfterFromOrJoin As Regex = New Regex("\b(?:FROM|JOIN)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled)

    ''' <summary>
    ''' (1) No string literal under either bridge project carries a SQL keyword: every query lives in Core (FR-303, Article XI).
    ''' </summary>
    <Fact>
    Public Sub TheBridgeProjectsHoldNoSqlLiteral()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim offenders As List(Of String) = New List(Of String)()
        For Each folder As String In New String() {Path.Combine(root, "src", "CodeMem.Bridge"), Path.Combine(root, "src", "CodeMem.Bridging")}
            If Not Directory.Exists(folder) Then Continue For
            For Each file As String In SqlLocationGateTests.SourceFiles(folder)
                For Each literal As String In LiteralsOf(file)
                    If SqlKeyword.IsMatch(literal) Then offenders.Add(Path.GetFileName(file) & ": " & literal)
                Next
            Next
        Next
        Assert.True(offenders.Count = 0, String.Join(Environment.NewLine, offenders))
    End Sub

    ''' <summary>
    ''' (2) The registry module under Core/Repositories/Registry holds at least one SELECT (the vacuous guard), no write keyword, no
    ''' sqlite_master, and every table after FROM or JOIN is code_map_solutions (FR-304, INC1).
    ''' </summary>
    <Fact>
    Public Sub RegistryReadsNameOnlyTheRegistryTable()
        Dim folder As String = Path.Combine(RepoPaths.RepositoryRoot(), "src", "CodeMem.Core", "Repositories", "Registry")
        Dim literals As List(Of String) = New List(Of String)()
        If Directory.Exists(folder) Then
            For Each file As String In Directory.GetFiles(folder, "*.vb")
                literals.AddRange(LiteralsOf(file))
            Next
        End If
        Dim selects As Integer = 0
        For Each literal As String In literals
            If literal.TrimStart().StartsWith("SELECT", StringComparison.Ordinal) Then selects += 1
        Next
        Assert.True(selects >= 1, "no SELECT literal under Core/Repositories/Registry: the scan is vacuous")
        For Each literal As String In literals
            Assert.False(WriteKeyword.IsMatch(literal), "a write keyword in the registry module: " & literal)
            Assert.DoesNotContain("sqlite_master", literal, StringComparison.OrdinalIgnoreCase)
            For Each table As Match In TableAfterFromOrJoin.Matches(literal)
                Assert.Equal("code_map_solutions", table.Groups(1).Value)
            Next
        Next
    End Sub

    ''' <summary>
    ''' (3) Every literal inside a Function Read… or Function Search… of Core/Repositories/*.vb, joined in order, begins with SELECT or WITH
    ''' and carries no write keyword; at least one such function is found (the vacuous guard).
    ''' </summary>
    <Fact>
    Public Sub CoreReadMethodsAreSelectOnly()
        Dim folder As String = Path.Combine(RepoPaths.RepositoryRoot(), "src", "CodeMem.Core", "Repositories")
        Dim checked As Integer = 0
        Dim failures As List(Of String) = New List(Of String)()
        For Each file As String In Directory.GetFiles(folder, "*.vb")
            Dim text As String = CodeText(file)
            Dim declarations As MatchCollection = Declaration.Matches(text)
            Dim joinedByFunction As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
            Dim order As List(Of String) = New List(Of String)()
            For Each literal As Match In StringLiteral.Matches(text)
                Dim enclosing As String = Nothing
                For Each declaration As Match In declarations
                    If declaration.Index < literal.Index Then enclosing = declaration.Groups(1).Value
                Next
                If enclosing Is Nothing Then Continue For
                If Not (enclosing.StartsWith("Read", StringComparison.Ordinal) OrElse enclosing.StartsWith("Search", StringComparison.Ordinal)) Then Continue For
                If Not joinedByFunction.ContainsKey(enclosing) Then
                    joinedByFunction(enclosing) = ""
                    order.Add(enclosing)
                End If
                joinedByFunction(enclosing) &= Unquote(TripwireTests.Normalise(literal.Value))
            Next
            For Each name As String In order
                checked += 1
                Dim joined As String = joinedByFunction(name).Trim()
                If Not (joined.StartsWith("SELECT", StringComparison.Ordinal) OrElse joined.StartsWith("WITH", StringComparison.Ordinal)) Then
                    failures.Add(Path.GetFileName(file) & "." & name & " does not begin with SELECT or WITH: " & joined)
                End If
                If WriteKeyword.IsMatch(joined) Then failures.Add(Path.GetFileName(file) & "." & name & " carries a write keyword: " & joined)
            Next
        Next
        Assert.True(checked >= 1, "no Read/Search function with a literal found under Core/Repositories: the scan is vacuous")
        Assert.True(failures.Count = 0, String.Join(Environment.NewLine, failures))
    End Sub

    ''' <summary>
    ''' (4) The identifiers StoreDatabase and CodeMapSolutionsRepository appear, outside their own definitions, only under src/CodeMem.Bridging,
    ''' src/CodeMem.Bridge and tests: nothing in Core, Extraction or the Extractor opens the store (constitution v1.3.0, Article IX).
    ''' </summary>
    <Fact>
    Public Sub TheStoreIsOpenedOnlyByTheBridge()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim identifiers As String() = New String() {"StoreDatabase", "CodeMapSolutionsRepository"}
        Dim offenders As List(Of String) = New List(Of String)()
        Dim files As List(Of String) = SqlLocationGateTests.SourceFiles(Path.Combine(root, "src"))
        files.AddRange(SqlLocationGateTests.SourceFiles(Path.Combine(root, "tests")))
        For Each file As String In files
            Dim normalized As String = file.Replace("\"c, "/"c)
            If normalized.Contains("/CodeMem.Bridging/") OrElse normalized.Contains("/CodeMem.Bridge/") OrElse normalized.Contains("/tests/") Then Continue For
            If normalized.Contains("/CodeMem.Core/Repositories/Registry/") Then Continue For
            Dim name As String = Path.GetFileName(file)
            Dim text As String = CodeText(file)
            For Each identifier As String In identifiers
                If String.Equals(name, identifier & ".vb", StringComparison.Ordinal) Then Continue For
                If Regex.IsMatch(text, "\b" & identifier & "\b") Then offenders.Add(name & ": " & identifier)
            Next
        Next
        Assert.True(offenders.Count = 0, String.Join(Environment.NewLine, offenders))
    End Sub

    ''' <summary>
    ''' The file's code lines only: whole-line comments are dropped, because XML documentation quotes member names and pairs of quotes
    ''' across comment lines would read as literals.
    ''' </summary>
    ''' <param name="file">The source file.</param>
    ''' <returns>The code text.</returns>
    Private Shared Function CodeText(file As String) As String
        Dim kept As List(Of String) = New List(Of String)()
        For Each line As String In IO.File.ReadAllLines(file)
            If Not line.TrimStart().StartsWith("'", StringComparison.Ordinal) Then kept.Add(line)
        Next
        Return String.Join(vbLf, kept)
    End Function

    Private Shared Function LiteralsOf(file As String) As List(Of String)
        Dim literals As List(Of String) = New List(Of String)()
        For Each match As Match In StringLiteral.Matches(CodeText(file))
            literals.Add(Unquote(TripwireTests.Normalise(match.Value)))
        Next
        Return literals
    End Function

    Private Shared Function Unquote(literal As String) As String
        Dim inner As String = literal.Substring(1, literal.Length - 2)
        Return inner.Replace("""""", """")
    End Function

End Class

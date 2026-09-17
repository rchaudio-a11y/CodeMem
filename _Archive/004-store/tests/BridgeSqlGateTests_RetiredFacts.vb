' File: BridgeSqlGateTests_RetiredFacts.vb
' Project: _Archive/004-store/tests (not compiled)
' Description: The two store-side facts of BridgeSqlGateTests: the registry module's literals name code_map_solutions only; the store is opened only by the bridge.
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Archived by feature 005 (T024; Article XIV): the registry module and the store database left the tree (see ../README.md). (1) and (3)
' remain in tests/CodeMem.Tests/Guards/BridgeSqlGateTests.vb. The helpers these used (LiteralsOf, CodeText, WriteKeyword,
' TableAfterFromOrJoin) are the class's own; TableAfterFromOrJoin left with (2).

' ---- BridgeSqlGateTests (2) RegistryReadsNameOnlyTheRegistryTable ----
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


' ---- BridgeSqlGateTests (4) TheStoreIsOpenedOnlyByTheBridge ----
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


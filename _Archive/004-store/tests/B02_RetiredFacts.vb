' File: B02_RetiredFacts.vb
' Project: _Archive/004-store/tests (not compiled)
' Description: The registry-era facts of B02_PortedReaderTests: symbol_search by projectId, the registry-absent refusal, both scopes, the no-registry-row reason.
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Archived by feature 005 (T017; Article XIV): these facts asserted the code_map_solutions registry the bridge no longer reads. They are
' fragments of the class they came from, kept as code, not compiled. The scenario members they used (Registry, RegistryFixture.WithoutTable,
' the Ghost / Unbound / Retired rows) left with the registry.

' ---- B02 (2) SymbolSearchByKeyAndByProjectAgree ----
    ''' <summary>
    ''' (2) symbol_search by key and by project return the same rows and total; by project the registry counts are numbers, by key null;
    ''' projectSymbolId narrows the rows and never the total.
    ''' </summary>
    <Fact>
    Public Sub SymbolSearchByKeyAndByProjectAgree()
        Dim byKey As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "Widget"))
        Dim byProject As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("projectId", 131373L, "name", "Widget"))
        Assert.False(byKey.IsError, byKey.Text)
        Assert.False(byProject.IsError, byProject.Text)
        Assert.Equal(Canonical(byKey.Root().GetProperty("symbols")), Canonical(byProject.Root().GetProperty("symbols")))
        Assert.True(byKey.Root().GetProperty("symbols").GetArrayLength() > 0, "no Widget rows")
        Assert.Equal(byKey.Root().GetProperty("total").GetInt32(), byProject.Root().GetProperty("total").GetInt32())
        Assert.False(byKey.Root().GetProperty("truncated").GetBoolean())
        Assert.False(byProject.Root().GetProperty("truncated").GetBoolean())
        Dim projectScope As JsonElement = byProject.Root().GetProperty("scope")
        Assert.Equal("projectId", projectScope.GetProperty("by").GetString())
        For Each name As String In New String() {"activeRegistryRows", "unboundRegistryRows", "inactiveRegistryRows"}
            Assert.Equal(JsonValueKind.Number, projectScope.GetProperty(name).ValueKind)
        Next
        Assert.Equal(JsonValueKind.Array, projectScope.GetProperty("danglingSolutionIds").ValueKind)
        Assert.False(projectScope.GetProperty("hadNothingToSearch").GetBoolean())
        Dim keyScope As JsonElement = byKey.Root().GetProperty("scope")
        Assert.Equal("solutionKey", keyScope.GetProperty("by").GetString())
        For Each name As String In New String() {"activeRegistryRows", "unboundRegistryRows", "inactiveRegistryRows", "danglingSolutionIds"}
            Assert.Equal(JsonValueKind.Null, keyScope.GetProperty(name).ValueKind)
        Next
        Dim libProject As Long = _scenario.SymbolId("Project:Sample.Lib")
        Dim narrowed As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "Widget", "projectSymbolId", libProject))
        Assert.False(narrowed.IsError, narrowed.Text)
        Assert.Equal(byKey.Root().GetProperty("total").GetInt32(), narrowed.Root().GetProperty("total").GetInt32())
        Assert.True(narrowed.Root().GetProperty("symbols").GetArrayLength() < byKey.Root().GetProperty("symbols").GetArrayLength(), "the project filter narrowed nothing")
        For Each row As JsonElement In narrowed.Root().GetProperty("symbols").EnumerateArray()
            Assert.Equal(libProject, row.GetProperty("project").GetProperty("id").GetInt64())
        Next
    End Sub

' ---- B02 (7) registry absent ----
        Using noTable As RegistryFixture = RegistryFixture.WithoutTable()
            Expect("registry absent", New BridgeHost(BridgeHost.WriteConfig(_scenario.Map.Path, Nothing, False, False)).Invoke("symbol_search", Args("projectId", 131373L, "name", "x")), "no code_map_solutions table")
        End Using

' ---- B02 (7) both scopes ----
        Expect("both scopes", host.Invoke("symbol_search", Args("projectId", 131373L, "solutionKey", "Sample", "name", "x")), "Both")

' ---- B02 (7) no registry row names project N ----
        Dim nothingToSearch As BridgeReply = host.Invoke("symbol_search", Args("projectId", 424242L, "name", "x"))
        Assert.False(nothingToSearch.IsError, nothingToSearch.Text)
        Dim scope As JsonElement = nothingToSearch.Root().GetProperty("scope")
        Assert.True(scope.GetProperty("hadNothingToSearch").GetBoolean())
        Assert.Contains("424242", scope.GetProperty("reason").GetString())
        Assert.Equal(0, nothingToSearch.Root().GetProperty("total").GetInt32())


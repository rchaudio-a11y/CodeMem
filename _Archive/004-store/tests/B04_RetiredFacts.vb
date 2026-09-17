' File: B04_RetiredFacts.vb
' Project: _Archive/004-store/tests (not compiled)
' Description: The registry-era facts of B04_MapStatusTests: map_missing_solution with the unbound and inactive listing, and the empty registry.
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Archived by feature 005 (T017; Article XIV): these facts asserted the code_map_solutions registry the bridge no longer reads. They are
' fragments of the class they came from, kept as code, not compiled. The scenario members they used (Registry, RegistryFixture.WithoutTable,
' the Ghost / Unbound / Retired rows) left with the registry.

    ''' <summary>
    ''' (6) A row bound to an id the map lacks is map_missing_solution naming key and id; unbound and inactive rows are listed with no
    ''' verdict; bound counts the active bound rows.
    ''' </summary>
    <Fact>
    Public Sub MapMissingSolutionUnboundAndInactiveAreListed()
        Dim root As JsonElement = _scenario.AtC1.RootElement
        Dim ghost As JsonElement = MapStatusScenario.Entry(_scenario.AtC1, "Ghost")
        Assert.Equal("map_missing_solution", ghost.GetProperty("verdict").GetString())
        Assert.Contains("Ghost", ghost.GetProperty("reason").GetString())
        Assert.Contains("99", ghost.GetProperty("reason").GetString())
        Assert.Equal(JsonValueKind.Null, ghost.GetProperty("run").ValueKind)
        Assert.Equal(2, root.GetProperty("bound").GetInt32())
        Assert.Equal(1, root.GetProperty("unbound").GetArrayLength())
        Assert.Equal("Unbound", root.GetProperty("unbound")(0).GetProperty("solutionKey").GetString())
        Assert.Equal(1, root.GetProperty("inactive").GetArrayLength())
        Assert.Equal("Retired", root.GetProperty("inactive")(0).GetProperty("solutionKey").GetString())
        Assert.Equal("inactive", root.GetProperty("inactive")(0).GetProperty("state").GetString())
    End Sub

    ''' <summary>
    ''' (9) An empty registry is a state, not a refusal: no entries, bound 0, unbound and inactive empty.
    ''' </summary>
    <Fact>
    Public Sub AnEmptyRegistryIsAStateNotARefusal()
        Using registry As RegistryFixture = New RegistryFixture()
            Dim reply As BridgeReply = New BridgeHost(BridgeHost.WriteConfig(_scenario.Map.Path, registry.Path, Nothing, False, False)).Invoke("map_status", New Dictionary(Of String, Object)())
            Assert.False(reply.IsError, reply.Text)
            Dim root As JsonElement = reply.Root()
            Assert.Equal(0, root.GetProperty("entries").GetArrayLength())
            Assert.Equal(0, root.GetProperty("bound").GetInt32())
            Assert.Equal(0, root.GetProperty("unbound").GetArrayLength())
            Assert.Equal(0, root.GetProperty("inactive").GetArrayLength())
        End Using
    End Sub

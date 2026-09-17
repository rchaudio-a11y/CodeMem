' File: B05_RetiredFacts.vb
' Project: _Archive/004-store/tests (not compiled)
' Description: The registry-era facts of B05_ExtractGateTests: the unbound, inactive and ghost key refusals, and the empty registry.
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Archived by feature 005 (T019; Article XIV): these facts asserted the code_map_solutions registry the bridge no longer reads. They are
' fragments of the class they came from, kept as code, not compiled. The scenario members they used (Registry, NestedRegistry, the
' Unbound / Retired / Ghost rows) left with the registry.

' ---- B05 (9) KeyRefusals, the 004 body ----
    ''' <summary>
    ''' (9) Key refusals by name: unknown, unbound, inactive, bound to an id the map lacks; none launches, the map byte-identical (SC-306).
    ''' </summary>
    <Fact>
    Public Sub KeyRefusals()
        Dim host As BridgeHost = _scenario.Host(True, True)
        Dim before As String = MapSnapshot.FileBytesHash(_scenario.Map.Path)
        Expect("unknown key", host.Invoke("extract", Args("solutionKey", "Nope")), "No code_map_solutions row")
        Expect("unbound", host.Invoke("extract", Args("solutionKey", "Unbound")), "no codemem_solution_id")
        Expect("inactive", host.Invoke("extract", Args("solutionKey", "Retired")), "inactive")
        Expect("ghost", host.Invoke("extract", Args("solutionKey", "Ghost")), "holds no solution 99")
        Assert.Equal(0, host.Launcher.Requests.Count)
        Assert.Equal(before, MapSnapshot.FileBytesHash(_scenario.Map.Path))
    End Sub

' ---- B05 (17) AnEmptyRegistryRefusesEveryTarget ----
    ''' <summary>
    ''' (17) An empty registry refuses every target: the key is not registered, no root contains the path, stale considers nothing; no launch.
    ''' </summary>
    <Fact>
    Public Sub AnEmptyRegistryRefusesEveryTarget()
        Using empty As RegistryFixture = New RegistryFixture()
            Dim host As BridgeHost = New BridgeHost(_scenario.Config(empty, True, True))
            Expect("key", host.Invoke("extract", Args("solutionKey", "Sample")), "No code_map_solutions row")
            Expect("path", host.Invoke("extract", Args("repoPath", _scenario.Root)), "No registered solution's repository root contains")
            Dim stale As BridgeReply = host.Invoke("extract", Args("stale", True))
            Assert.False(stale.IsError, stale.Text)
            Assert.Equal(0, stale.Root().GetProperty("considered").GetArrayLength())
            Assert.Equal(0, stale.Root().GetProperty("extractedCount").GetInt32())
            Assert.Equal(0, host.Launcher.Requests.Count)
        End Using
    End Sub

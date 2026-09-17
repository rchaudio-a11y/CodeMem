' File: B08_RetiredFacts.vb
' Project: _Archive/004-store/tests (not compiled)
' Description: The registry-era fact of B08_LiveMapTests: symbol_search by projectId resolving through the store's registry.
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Archived by feature 005 (T023; Article XIV): the bridge takes no projectId and opens no store. The fact asked again by solutionKey is
' B08 (4) LiveSearchByKeyResolvesFromTheMap. The class was armed by CODEMEM_LIVE_MAP and CODEMEM_LIVE_STORE; only the map remains.

' ---- B08 (4) LiveSearchByProjectResolvesThroughTheRegistry, the 004 body ----
    ''' <summary>
    ''' (4) symbol_search by projectId 132040 resolves through the registry to GameRoom: btnDeal_Click is one row, id 584.
    ''' </summary>
    <SkippableFact>
    Public Sub LiveSearchByProjectResolvesThroughTheRegistry()
        Dim live As LiveBridge = Arm()
        Dim reply As BridgeReply = Timed(live, "symbol_search", Args("projectId", 132040L, "name", "btnDeal_Click"))
        Assert.Equal("projectId", reply.Root().GetProperty("scope").GetProperty("by").GetString())
        Assert.Equal(1, reply.Root().GetProperty("symbols").GetArrayLength())
        Assert.Equal(584L, reply.Root().GetProperty("symbols")(0).GetProperty("id").GetInt64())
        live.AssertUnchanged()
    End Sub

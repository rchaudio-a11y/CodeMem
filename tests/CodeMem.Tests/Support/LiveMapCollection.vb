' File: LiveMapCollection.vb
' Project: CodeMem.Tests
' Description: Declares the LiveMap xUnit collection with parallelization disabled: B08's timing facts (SC-301, each call under 3 s) measure the bridge, not the suite's load (feature 004, T054).
' Author: RCH Automation LLC
' Created: 2026-09-16

Imports Xunit

''' <summary>
''' The <c>LiveMap</c> collection definition. Its classes run while no other collection runs: the armed whole-suite run of 2026-09-16
''' measured map_status at 4108 ms under the parallel load of the extraction scenarios where it takes ~150 ms alone.
''' </summary>
<CollectionDefinition("LiveMap", DisableParallelization:=True)>
Public Class LiveMapCollection
End Class

' File: ExtractLogCollection.vb
' Project: CodeMem.Tests
' Description: Declares the ExtractLog xUnit collection: B05 and B06 both append to the one extract.log beside the test host, so they run in sequence (analyze pass 2, U2).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T026): carries AddedSolutionFixture - the one real launch B10 (10), (11) and B11 (4) share, made once per run of the
' collection (analyze I3).

Imports Xunit

''' <summary>
''' The <c>ExtractLog</c> collection definition. Classes in it never run concurrently with each other.
''' </summary>
<CollectionDefinition("ExtractLog")>
Public Class ExtractLogCollection
    Implements ICollectionFixture(Of AddedSolutionFixture)
End Class

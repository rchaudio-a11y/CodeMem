' File: ExtractLogCollection.vb
' Project: CodeMem.Tests
' Description: Declares the ExtractLog xUnit collection: B05 and B06 both append to the one extract.log beside the test host, so they run in sequence (analyze pass 2, U2).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports Xunit

''' <summary>
''' The <c>ExtractLog</c> collection definition. Classes in it never run concurrently with each other.
''' </summary>
<CollectionDefinition("ExtractLog")>
Public Class ExtractLogCollection
End Class

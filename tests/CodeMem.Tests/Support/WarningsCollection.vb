' File: WarningsCollection.vb
' Project: CodeMem.Tests
' Description: The xUnit collection that shares one restored WarningsFixture across the warning facts (feature 005, X02).
' Author: RCH Automation LLC
' Created: 2026-09-17

Imports Xunit

''' <summary>
''' Declares the "Warnings" collection; the fixture restores the two warning projects once, offline (research R71).
''' </summary>
<CollectionDefinition("Warnings")>
Public Class WarningsCollection
    Implements ICollectionFixture(Of WarningsFixture)

End Class

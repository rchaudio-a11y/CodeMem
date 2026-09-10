' File: FixtureCollection.vb
' Project: CodeMem.Tests
' Description: Declares the Fixture xUnit collection whose tests share one restored fixture solution.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Xunit

''' <summary>
''' The <c>Fixture</c> collection definition. Tests in one collection run sequentially and share <see cref="FixtureSolution"/>.
''' </summary>
<CollectionDefinition("Fixture")>
Public Class FixtureCollection
    Implements ICollectionFixture(Of FixtureSolution)
End Class

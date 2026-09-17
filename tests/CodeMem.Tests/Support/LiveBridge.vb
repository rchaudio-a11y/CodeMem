' File: LiveBridge.vb
' Project: CodeMem.Tests
' Description: An in-process host over the live map and store with the map's hash taken at arming, for the B08 facts (feature 004).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Bridging
Imports CodeMem.Core
Imports Xunit

''' <summary>
''' Both gates off: nothing reachable through this host launches anything. <see cref="AssertUnchanged"/> compares the live map's hash.
''' </summary>
Public Class LiveBridge

    ''' <summary>The host.</summary>
    Public ReadOnly Property Host As BridgeHost

    ''' <summary>The live map path.</summary>
    Public ReadOnly Property MapPath As String

    Private ReadOnly _before As String

    ''' <summary>
    ''' Writes a temp configuration naming the live paths and hashes the map.
    ''' </summary>
    ''' <param name="mapPath">The live map.</param>
    ''' <param name="storePath">The live store.</param>
    Public Sub New(mapPath As String, storePath As String)
        Me.MapPath = mapPath
        Host = New BridgeHost(BridgeHost.WriteConfig(mapPath, Nothing, False, False))
        _before = MapSnapshot.FileBytesHash(mapPath)
    End Sub

    ''' <summary>
    ''' Asserts the live map's bytes are what they were at arming.
    ''' </summary>
    Public Sub AssertUnchanged()
        Assert.Equal(_before, MapSnapshot.FileBytesHash(MapPath))
    End Sub

    ''' <summary>
    ''' The active symbol rows of one solution, read through the bridge's own read-only door (B08 (7); never a writable connection).
    ''' </summary>
    ''' <param name="solutionKey">The solution.</param>
    ''' <returns>The count.</returns>
    Public Function ActiveSymbolCount(solutionKey As String) As Integer
        Using map As MapDatabase = MapAccess.OpenRead(BridgeConfigFile.Load(Host.ConfigPath))
            Dim solution As SolutionRecord = SolutionsRepository.ReadByKey(map, solutionKey)
            Dim count As Integer = CodeSymbolsRepository.Search(map, New List(Of Long) From {solution.Id}, Nothing, Nothing).Count
            map.EndRead()
            Return count
        End Using
    End Function

End Class

' File: ScopeResolver.vb
' Project: CodeMem.Bridging
' Description: The one door for scope: solutionKey, resolved against solutions.key on the call's open map; a call carrying projectId is refused by name (005 FR-405, FR-406; data-model §3).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T014): the registry branch is gone. RefuseProjectId reads the call's raw arguments (research R68): the protocol
' layer ignores an argument no parameter names, so the refusal by name needs the dictionary, not the bound parameters.

Imports System.Text.Json
Imports CodeMem.Core

''' <summary>
''' <see cref="ValidateArguments"/> is the argument stage (before the configuration is read, INC2); <see cref="Resolve"/> runs on the call's
''' open map.
''' </summary>
Public Module ScopeResolver

    ''' <summary>
    ''' A call carrying projectId - in any letter case - is refused by name, whatever else it carries (FR-406).
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <exception cref="BridgeRefusalException">ProjectIdRemoved.</exception>
    Public Sub RefuseProjectId(rawArguments As IReadOnlyDictionary(Of String, JsonElement))
        If rawArguments Is Nothing Then Return
        For Each key As String In rawArguments.Keys
            If String.Equals(key, "projectId", StringComparison.OrdinalIgnoreCase) Then Throw Refuse(BridgeRefusalKind.ProjectIdRemoved, Nothing)
        Next
    End Sub

    ''' <summary>
    ''' projectId is refused first; then a missing key is refused by name.
    ''' </summary>
    ''' <param name="rawArguments">The call's arguments as received, or Nothing.</param>
    ''' <param name="solutionKey">The solution key, or Nothing.</param>
    ''' <exception cref="BridgeRefusalException">ProjectIdRemoved or ScopeMissing.</exception>
    Public Sub ValidateArguments(rawArguments As IReadOnlyDictionary(Of String, JsonElement), solutionKey As String)
        RefuseProjectId(rawArguments)
        If solutionKey Is Nothing Then Throw Refuse(BridgeRefusalKind.ScopeMissing, Nothing)
    End Sub

    ''' <summary>
    ''' Resolves the one map solution with the key (SolutionKeyUnknown otherwise).
    ''' </summary>
    ''' <param name="solutionKey">The solution key.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="map">The call's open map.</param>
    ''' <returns>The resolved scope.</returns>
    Public Function Resolve(solutionKey As String, config As BridgeConfig, map As MapDatabase) As ResolvedScope
        Dim solution As SolutionRecord = SolutionsRepository.ReadByKey(map, solutionKey)
        If solution Is Nothing Then
            Throw Refuse(BridgeRefusalKind.SolutionKeyUnknown, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"mapPath", config.MapPath}, {"key", solutionKey}})
        End If
        Dim envelope As ScopeEnvelope = New ScopeEnvelope With {
            .By = "solutionKey",
            .SolutionKey = solutionKey,
            .Solutions = New List(Of ScopeSolutionEnvelope) From {New ScopeSolutionEnvelope With {.SolutionId = solution.Id, .Key = solution.Key}}}
        Return New ResolvedScope With {.Envelope = envelope, .SolutionIds = New List(Of Long) From {solution.Id}}
    End Function

    Private Function Refuse(kind As BridgeRefusalKind, facts As IDictionary(Of String, String)) As BridgeRefusalException
        Return New BridgeRefusalException(BridgeRefusal.Named(kind, facts))
    End Function

End Module

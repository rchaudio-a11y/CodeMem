' File: ScopeResolver.vb
' Project: CodeMem.Bridging
' Description: The one door for scope: exactly one of projectId or solutionKey, resolved through the registry or the map (FR-310, spec Q7; 058 §3.1, data-model §3).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core

''' <summary>
''' <see cref="ValidateArguments"/> is the argument stage (before the configuration is read, INC2); <see cref="Resolve"/> runs on the call's
''' open map with the registry rows read at the store stage.
''' </summary>
Public Module ScopeResolver

    ''' <summary>
    ''' Neither or both scope arguments is refused by name.
    ''' </summary>
    ''' <param name="projectId">The project id, or Nothing.</param>
    ''' <param name="solutionKey">The solution key, or Nothing.</param>
    ''' <exception cref="BridgeRefusalException">ScopeMissing or ScopeConflict.</exception>
    Public Sub ValidateArguments(projectId As Long?, solutionKey As String)
        If Not projectId.HasValue AndAlso solutionKey Is Nothing Then Throw Refuse(BridgeRefusalKind.ScopeMissing, Nothing)
        If projectId.HasValue AndAlso solutionKey IsNot Nothing Then Throw Refuse(BridgeRefusalKind.ScopeConflict, Nothing)
    End Sub

    ''' <summary>
    ''' Resolves the scope. By key: the one map solution (SolutionKeyUnknown otherwise), the four registry fields null. By project: every
    ''' active registry row of that project whose binding names an existing map solution; the four counts describe the rows with that
    ''' project id; a bound id with no map row is dangling; no solution is not an error (hadNothingToSearch with the reason).
    ''' </summary>
    ''' <param name="projectId">The project id, or Nothing.</param>
    ''' <param name="solutionKey">The solution key, or Nothing.</param>
    ''' <param name="registry">The registry rows read at the store stage; required when projectId is given.</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="map">The call's open map.</param>
    ''' <returns>The resolved scope.</returns>
    Public Function Resolve(projectId As Long?, solutionKey As String, registry As List(Of RegistryRecord), config As BridgeConfig, map As MapDatabase) As ResolvedScope
        Dim envelope As ScopeEnvelope = New ScopeEnvelope With {.Solutions = New List(Of ScopeSolutionEnvelope)()}
        Dim ids As List(Of Long) = New List(Of Long)()
        If solutionKey IsNot Nothing Then
            Dim solution As SolutionRecord = SolutionsRepository.ReadByKey(map, solutionKey)
            If solution Is Nothing Then
                Throw Refuse(BridgeRefusalKind.SolutionKeyUnknown, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"mapPath", config.MapPath}, {"key", solutionKey}})
            End If
            envelope.By = "solutionKey"
            envelope.SolutionKey = solutionKey
            envelope.Solutions.Add(New ScopeSolutionEnvelope With {.SolutionId = solution.Id, .Key = solution.Key})
            ids.Add(solution.Id)
            Return New ResolvedScope With {.Envelope = envelope, .SolutionIds = ids}
        End If
        envelope.By = "projectId"
        envelope.ProjectId = projectId
        envelope.DanglingSolutionIds = New List(Of Long)()
        Dim active As Integer = 0
        Dim unbound As Integer = 0
        Dim inactive As Integer = 0
        Dim rowsForProject As Integer = 0
        For Each row As RegistryRecord In registry
            If row.ProjectId <> projectId.Value Then Continue For
            rowsForProject += 1
            If Not String.Equals(row.State, "active", StringComparison.Ordinal) Then
                inactive += 1
                Continue For
            End If
            active += 1
            If Not row.CodememSolutionId.HasValue Then
                unbound += 1
                Continue For
            End If
            Dim solution As SolutionRecord = SolutionsRepository.ReadById(map, row.CodememSolutionId.Value)
            If solution Is Nothing Then
                envelope.DanglingSolutionIds.Add(row.CodememSolutionId.Value)
                Continue For
            End If
            envelope.Solutions.Add(New ScopeSolutionEnvelope With {.SolutionId = solution.Id, .Key = solution.Key})
            ids.Add(solution.Id)
        Next
        envelope.Solutions.Sort(Function(a As ScopeSolutionEnvelope, b As ScopeSolutionEnvelope) String.CompareOrdinal(a.Key, b.Key))
        envelope.ActiveRegistryRows = active
        envelope.UnboundRegistryRows = unbound
        envelope.InactiveRegistryRows = inactive
        envelope.HadNothingToSearch = ids.Count = 0
        If envelope.HadNothingToSearch Then
            Dim n As String = projectId.Value.ToString(Globalization.CultureInfo.InvariantCulture)
            envelope.Reason = If(rowsForProject = 0, "no registry row names project " & n, "every row for project " & n & " is unbound, inactive or bound to a solution the map lacks")
        End If
        Return New ResolvedScope With {.Envelope = envelope, .SolutionIds = ids}
    End Function

    Private Function Refuse(kind As BridgeRefusalKind, facts As IDictionary(Of String, String)) As BridgeRefusalException
        Return New BridgeRefusalException(BridgeRefusal.Named(kind, facts))
    End Function

End Module

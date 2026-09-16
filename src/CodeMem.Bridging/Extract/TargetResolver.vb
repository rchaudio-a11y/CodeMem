' File: TargetResolver.vb
' Project: CodeMem.Bridging
' Description: Resolves an extract target through the registry: a key exactly, or a path by directory containment against every bound solution's registered root, the longest root winning, a tie refused (FR-326, FR-327, spec Q1, COR1; data-model §8).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports CodeMem.Core
Imports CodeMem.Extraction

''' <summary>
''' The registered root is the map's repo_root of a bound solution (spec Q1); containment is SolutionScope.ContainsDirectory, the one door.
''' </summary>
Public Module TargetResolver

    ''' <summary>
    ''' Resolves a request naming a key or a path.
    ''' </summary>
    ''' <param name="request">The request (exactly one of solutionKey and repoPath set).</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="registry">The registry rows read at the store stage.</param>
    ''' <param name="map">The call's open map.</param>
    ''' <returns>The target.</returns>
    ''' <exception cref="BridgeRefusalException">KeyNotRegistered, KeyInactive, KeyUnbound, MapMissingSolution, PathNotRegistered or AmbiguousRoot.</exception>
    Public Function Resolve(request As ExtractRequest, config As BridgeConfig, registry As List(Of RegistryRecord), map As MapDatabase) As ResolvedTarget
        If request.SolutionKey IsNot Nothing Then Return ByKey(request.SolutionKey, config, registry, map)
        Return ByPath(request.RepoPath, config, registry, map)
    End Function

    Private Function ByKey(key As String, config As BridgeConfig, registry As List(Of RegistryRecord), map As MapDatabase) As ResolvedTarget
        Dim row As RegistryRecord = registry.Find(Function(r As RegistryRecord) String.Equals(r.SolutionKey, key, StringComparison.Ordinal))
        If row Is Nothing Then Throw Refuse(BridgeRefusalKind.KeyNotRegistered, "key", key)
        If Not String.Equals(row.State, "active", StringComparison.Ordinal) Then Throw Refuse(BridgeRefusalKind.KeyInactive, "key", key, "state", row.State)
        If Not row.CodememSolutionId.HasValue Then Throw Refuse(BridgeRefusalKind.KeyUnbound, "key", key)
        Dim solution As SolutionRecord = SolutionsRepository.ReadById(map, row.CodememSolutionId.Value)
        If solution Is Nothing Then
            Throw Refuse(BridgeRefusalKind.MapMissingSolution, "key", key, "id", row.CodememSolutionId.Value.ToString(Globalization.CultureInfo.InvariantCulture), "mapPath", config.MapPath)
        End If
        Return TargetOf(row, solution, config)
    End Function

    Private Function ByPath(repoPath As String, config As BridgeConfig, registry As List(Of RegistryRecord), map As MapDatabase) As ResolvedTarget
        Dim roots As List(Of String) = New List(Of String)()
        Dim best As List(Of RegistryRecord) = New List(Of RegistryRecord)()
        Dim bestSolution As SolutionRecord = Nothing
        Dim bestLength As Integer = -1
        For Each row As RegistryRecord In registry
            If Not String.Equals(row.State, "active", StringComparison.Ordinal) OrElse Not row.CodememSolutionId.HasValue Then Continue For
            Dim solution As SolutionRecord = SolutionsRepository.ReadById(map, row.CodememSolutionId.Value)
            If solution Is Nothing OrElse String.IsNullOrEmpty(solution.RepoRoot) Then Continue For
            Dim scope As SolutionScope = SolutionScope.Resolve(solution.RepoRoot, solution.RepoRoot)
            roots.Add(scope.Root)
            If Not scope.ContainsDirectory(repoPath) Then Continue For
            If scope.Root.Length > bestLength Then
                bestLength = scope.Root.Length
                best.Clear()
                best.Add(row)
                bestSolution = solution
            ElseIf scope.Root.Length = bestLength Then
                best.Add(row)
            End If
        Next
        If best.Count = 0 Then Throw Refuse(BridgeRefusalKind.PathNotRegistered, "path", repoPath, "roots", If(roots.Count = 0, "none", String.Join(", ", roots)))
        If best.Count > 1 Then
            Dim keys As List(Of String) = New List(Of String)()
            For Each row As RegistryRecord In best
                keys.Add(row.SolutionKey)
            Next
            Throw Refuse(BridgeRefusalKind.AmbiguousRoot, "path", repoPath, "keys", String.Join(", ", keys))
        End If
        Return TargetOf(best(0), bestSolution, config)
    End Function

    Private Function TargetOf(row As RegistryRecord, solution As SolutionRecord, config As BridgeConfig) As ResolvedTarget
        Return New ResolvedTarget With {.SolutionKey = row.SolutionKey, .SolutionPath = solution.LastSeenPath, .MapPath = config.MapPath, .SolutionId = solution.Id}
    End Function

    Private Function Refuse(kind As BridgeRefusalKind, ParamArray pairs As String()) As BridgeRefusalException
        Dim facts As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            facts(pairs(i)) = pairs(i + 1)
        Next
        Return New BridgeRefusalException(BridgeRefusal.Named(kind, facts))
    End Function

End Module

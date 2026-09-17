' File: TargetResolver.vb
' Project: CodeMem.Bridging
' Description: Resolves an extract target through the map alone: a key exactly against solutions.key, or a directory by containment against every map solution's root, the longest root winning, a tie refused, none answered not in the map with the command that adds it (005 FR-410, FR-411, FR-415; research R65, R66; Q4, Q6 as ruled).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T021): rewritten over solutions rows - the registry is gone. A solution's root is the extractor's own answer
' (MapStatusReader.ScopeOf: repo_root, else the solution file's directory) and containment is SolutionScope.ContainsDirectory, the one door.

Imports CodeMem.Core
Imports CodeMem.Extraction

''' <summary>
''' By key: the row, or SolutionKeyUnknown with the add remedy. By path: every row's scope asked, the longest containing root wins, equal
''' lengths are AmbiguousRoot, none is PathNotInMap or AmbiguousSolutionFile from the directory's own solution files.
''' </summary>
Public Module TargetResolver

    ''' <summary>
    ''' Resolves a request naming a key or a path.
    ''' </summary>
    ''' <param name="request">The request (exactly one of solutionKey and repoPath set).</param>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="map">The call's open map.</param>
    ''' <returns>The target.</returns>
    ''' <exception cref="BridgeRefusalException">SolutionKeyUnknown, PathNotInMap, AmbiguousSolutionFile or AmbiguousRoot.</exception>
    Public Function Resolve(request As ExtractRequest, config As BridgeConfig, map As MapDatabase) As ResolvedTarget
        If request.SolutionKey IsNot Nothing Then Return ByKey(request.SolutionKey, config, map)
        Return ByPath(request.RepoPath, config, map)
    End Function

    Private Function ByKey(key As String, config As BridgeConfig, map As MapDatabase) As ResolvedTarget
        Dim solution As SolutionRecord = SolutionsRepository.ReadByKey(map, key)
        If solution Is Nothing Then Throw Refuse(BridgeRefusalKind.SolutionKeyUnknown, "mapPath", config.MapPath, "key", key)
        Return TargetOf(solution, config)
    End Function

    Private Function ByPath(repoPath As String, config As BridgeConfig, map As MapDatabase) As ResolvedTarget
        Dim roots As List(Of String) = New List(Of String)()
        Dim best As List(Of SolutionRecord) = New List(Of SolutionRecord)()
        Dim bestLength As Integer = -1
        For Each solution As SolutionRecord In SolutionsRepository.ReadAll(map)
            Dim scope As SolutionScope = MapStatusReader.ScopeOf(solution)
            roots.Add(scope.Root)
            If Not scope.ContainsDirectory(repoPath) Then Continue For
            If scope.Root.Length > bestLength Then
                bestLength = scope.Root.Length
                best.Clear()
                best.Add(solution)
            ElseIf scope.Root.Length = bestLength Then
                best.Add(solution)
            End If
        Next
        If best.Count = 0 Then
            Dim inspection As SolutionFileInspection = SolutionFileSuggestion.Inspect(repoPath)
            Dim rootsText As String = If(roots.Count = 0, "none", String.Join(", ", roots))
            Throw New BridgeRefusalException(BridgeRefusal.Named(SolutionFileSuggestion.KindOf(inspection), SolutionFileSuggestion.Facts(inspection, repoPath, rootsText)))
        End If
        If best.Count > 1 Then
            Dim keys As List(Of String) = New List(Of String)()
            For Each solution As SolutionRecord In best
                keys.Add(solution.Key)
            Next
            Throw Refuse(BridgeRefusalKind.AmbiguousRoot, "path", repoPath, "keys", String.Join(", ", keys))
        End If
        Return TargetOf(best(0), config)
    End Function

    Private Function TargetOf(solution As SolutionRecord, config As BridgeConfig) As ResolvedTarget
        Return New ResolvedTarget With {.SolutionKey = solution.Key, .SolutionPath = solution.LastSeenPath, .MapPath = config.MapPath, .SolutionId = solution.Id}
    End Function

    Private Function Refuse(kind As BridgeRefusalKind, ParamArray pairs As String()) As BridgeRefusalException
        Dim facts As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            facts(pairs(i)) = pairs(i + 1)
        Next
        Return New BridgeRefusalException(BridgeRefusal.Named(kind, facts))
    End Function

End Module

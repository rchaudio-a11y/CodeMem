' File: Reconciler.vb
' Project: CodeMem.Core
' Description: Reconciles staged symbols against the registry snapshot in the Article VI precedence (data-model.md algorithm).
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' Step 1 duplicate guard, (A) identity match, (A') reactivation, step 4 retirement, (B) rename candidates, (C) proximity rank.
''' </summary>
Public Module Reconciler

    ''' <summary>
    ''' Reconciles observed symbols against the solution's active rows (and its retired rows for the same doc ids).
    ''' </summary>
    ''' <param name="staged">Observed symbols, namespaces already merged.</param>
    ''' <param name="snapshot">Active rows of the solution before this run.</param>
    ''' <param name="retired">Retired rows of the solution whose doc id is among the staged ids not in the snapshot.</param>
    ''' <returns>What publication must do, with the ten counts (residuals not computed here).</returns>
    Public Function Reconcile(staged As List(Of ObservedSymbol), snapshot As List(Of RegistryRow), retired As List(Of RegistryRow)) As ReconciliationResult
        ' Step 1: duplicate guard. Refuse rather than guess which row is which.
        Dim seen As Dictionary(Of String, ObservedSymbol) = New Dictionary(Of String, ObservedSymbol)(StringComparer.Ordinal)
        For Each symbol As ObservedSymbol In staged
            Dim earlier As ObservedSymbol = Nothing
            If seen.TryGetValue(symbol.DocCommentId, earlier) Then
                Throw New DuplicateDocCommentIdException(symbol.DocCommentId, earlier.Primary, symbol.Primary)
            End If
            seen.Add(symbol.DocCommentId, symbol)
        Next

        Dim result As ReconciliationResult = New ReconciliationResult()
        Dim active As Dictionary(Of String, RegistryRow) = New Dictionary(Of String, RegistryRow)(StringComparer.Ordinal)
        For Each row As RegistryRow In snapshot
            active(row.DocCommentId) = row
        Next

        ' The most recently retired row per doc id: the input is ordered last_seen_run_id desc, id desc, so first wins.
        Dim retiredByDocId As Dictionary(Of String, RegistryRow) = New Dictionary(Of String, RegistryRow)(StringComparer.Ordinal)
        For Each row As RegistryRow In retired
            If Not retiredByDocId.ContainsKey(row.DocCommentId) Then retiredByDocId(row.DocCommentId) = row
        Next

        ' Step 2: (A) identity match on doc-comment id against active rows. Step 3: (A') a miss reactivates the most recently
        ' retired row carrying the identity, on its id; otherwise the symbol is new.
        Dim matchedIds As HashSet(Of Long) = New HashSet(Of Long)()
        For Each symbol As ObservedSymbol In staged
            Dim row As RegistryRow = Nothing
            If active.TryGetValue(symbol.DocCommentId, row) Then
                result.Refreshes.Add(symbol)
                result.ExistingIds(symbol.DocCommentId) = row.Id
                matchedIds.Add(row.Id)
            ElseIf retiredByDocId.TryGetValue(symbol.DocCommentId, row) Then
                result.Reactivations.Add(symbol)
                result.ExistingIds(symbol.DocCommentId) = row.Id
            Else
                result.Inserts.Add(symbol)
            End If
        Next

        ' Step 4: every active row not observed this run is retired; the row stays.
        For Each row As RegistryRow In snapshot
            If Not matchedIds.Contains(row.Id) Then result.Retirements.Add(row)
        Next

        ' Step 5: (B) for each new symbol, the rows retired this run with equal kind, equal resolved container and equal body hash.
        ' Step 6: (C) proximity evidence and rank over that set. Proximity never creates a candidate.
        For Each symbol As ObservedSymbol In result.Inserts
            Dim containerId As Long? = Nothing
            Dim resolved As Long
            If symbol.ContainerDocCommentId IsNot Nothing AndAlso result.ExistingIds.TryGetValue(symbol.ContainerDocCommentId, resolved) Then containerId = resolved
            Dim satisfying As List(Of RegistryRow) = New List(Of RegistryRow)()
            For Each row As RegistryRow In result.Retirements
                If row.Kind = symbol.Kind AndAlso Nullable.Equals(row.ContainerId, containerId) AndAlso String.Equals(row.BodyHash, symbol.BodyHash, StringComparison.Ordinal) Then
                    satisfying.Add(row)
                End If
            Next
            If satisfying.Count > 0 Then result.Candidates.AddRange(ProximityRanker.Rank(symbol, satisfying))
        Next

        ' Step 7: counts. Residuals are the auditor's.
        result.Counts.SymbolsObserved = staged.Count
        result.Counts.SymbolsMatched = result.Refreshes.Count
        result.Counts.SymbolsReactivated = result.Reactivations.Count
        result.Counts.SymbolsNew = result.Inserts.Count
        result.Counts.SymbolsRetired = result.Retirements.Count
        result.Counts.RegistryActiveBefore = snapshot.Count
        result.Counts.NotesOrphaned = 0
        result.Counts.RenameCandidates = result.Candidates.Count
        Return result
    End Function

End Module

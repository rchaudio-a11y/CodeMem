' File: ProximityRanker.vb
' Project: CodeMem.Core
' Description: Article VI (C): offset-proximity evidence and rank for rename candidates; evidence only, never a threshold.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' Orders the retired rows that satisfy (B) for one new symbol and assigns ranks 1..n.
''' </summary>
Public Module ProximityRanker

    ''' <summary>
    ''' Builds one candidate per retired row, ordered by (same_path desc, offset_distance asc with nulls last, retired id asc), ranked from 1.
    ''' </summary>
    ''' <param name="newSymbol">The new symbol.</param>
    ''' <param name="retiredRows">The retired rows satisfying (B).</param>
    ''' <returns>The ranked candidates.</returns>
    Public Function Rank(newSymbol As ObservedSymbol, retiredRows As IEnumerable(Of RegistryRow)) As List(Of RenameCandidate)
        Dim candidates As List(Of RenameCandidate) = New List(Of RenameCandidate)()
        For Each row As RegistryRow In retiredRows
            Dim samePath As Boolean = String.Equals(row.Path, newSymbol.Primary.Path, StringComparison.Ordinal)
            Dim distance As Integer? = Nothing
            If samePath Then distance = Math.Abs(row.StartOffset - newSymbol.Primary.StartOffset)
            candidates.Add(New RenameCandidate With {
                .RetiredSymbolId = row.Id,
                .NewDocCommentId = newSymbol.DocCommentId,
                .BodyHash = newSymbol.BodyHash,
                .SamePath = samePath,
                .OffsetDistance = distance})
        Next
        candidates.Sort(Function(a As RenameCandidate, b As RenameCandidate)
                            If a.SamePath <> b.SamePath Then Return If(a.SamePath, -1, 1)
                            If a.OffsetDistance.HasValue AndAlso b.OffsetDistance.HasValue Then
                                Dim byDistance As Integer = a.OffsetDistance.Value.CompareTo(b.OffsetDistance.Value)
                                If byDistance <> 0 Then Return byDistance
                            ElseIf a.OffsetDistance.HasValue <> b.OffsetDistance.HasValue Then
                                Return If(a.OffsetDistance.HasValue, -1, 1)
                            End If
                            Return a.RetiredSymbolId.CompareTo(b.RetiredSymbolId)
                        End Function)
        For i As Integer = 0 To candidates.Count - 1
            candidates(i).Rank = i + 1
        Next
        Return candidates
    End Function

End Module

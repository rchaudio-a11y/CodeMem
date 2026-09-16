' File: TwinFolder.vb
' Project: CodeMem.Bridging
' Description: The presentation rule (FR-340, FR-341, spec Q2, research R50, COR3): raw rows in, one declaration per twin group out; the one module search and detail call.
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-15 (T021): a pass-through - one declaration per row. 2026-09-16 (T049): the fold, in memory, after the repository's read.

Imports CodeMem.Core

''' <summary>
''' A group folds only when its project ids are all present, all distinct and more than one: two overloads on one line under one project
''' stay two declarations, a partial type is one row already. Twins are ordered by symbol id; the declaration's top-level labels are the
''' first twin's. The 200 cap and total count declarations, not rows, and are applied by the caller.
''' </summary>
Public Module TwinFolder

    ''' <summary>
    ''' Folds raw rows into presented declarations, in the order their first rows arrived.
    ''' </summary>
    ''' <param name="rows">Active rows in the repository's order.</param>
    ''' <returns>The declarations.</returns>
    Public Function Fold(rows As List(Of SymbolRecord)) As List(Of DeclarationEnvelope)
        Dim groups As Dictionary(Of String, List(Of SymbolRecord)) = New Dictionary(Of String, List(Of SymbolRecord))(StringComparer.Ordinal)
        Dim order As List(Of String) = New List(Of String)()
        For Each row As SymbolRecord In rows
            Dim key As String = KeyOf(row)
            Dim group As List(Of SymbolRecord) = Nothing
            If Not groups.TryGetValue(key, group) Then
                group = New List(Of SymbolRecord)()
                groups(key) = group
                order.Add(key)
            End If
            group.Add(row)
        Next
        Dim declarations As List(Of DeclarationEnvelope) = New List(Of DeclarationEnvelope)()
        For Each key As String In order
            Dim group As List(Of SymbolRecord) = groups(key)
            If FoldsTogether(group) Then
                Dim twins As List(Of SymbolRecord) = ById(group)
                declarations.Add(DeclarationOf(twins(0), EntriesOf(twins)))
            Else
                For Each row As SymbolRecord In group
                    declarations.Add(DeclarationOf(row, New List(Of CompiledIntoEnvelope) From {CompiledIntoOf(row)}))
                Next
            End If
        Next
        Return declarations
    End Function

    ''' <summary>
    ''' The compiledInto header of one row given its candidate twins: the row's own entry first, then the others by symbol id when the
    ''' group folds; the row's entry alone when it does not.
    ''' </summary>
    ''' <param name="row">The requested row.</param>
    ''' <param name="twins">The active rows of its solution sharing its four fields, itself included.</param>
    ''' <returns>The header entries.</returns>
    Public Function HeaderOf(row As SymbolRecord, twins As List(Of SymbolRecord)) As List(Of CompiledIntoEnvelope)
        Dim entries As List(Of CompiledIntoEnvelope) = New List(Of CompiledIntoEnvelope) From {CompiledIntoOf(row)}
        If twins IsNot Nothing AndAlso FoldsTogether(twins) Then
            For Each twin As SymbolRecord In ById(twins)
                If twin.Id <> row.Id Then entries.Add(CompiledIntoOf(twin))
            Next
        End If
        Return entries
    End Function

    ''' <summary>
    ''' Whether a group of rows sharing the four fields folds: more than one row, every row compiled into a project, no project twice.
    ''' </summary>
    ''' <param name="group">The rows.</param>
    ''' <returns>True when they present as one declaration.</returns>
    Public Function FoldsTogether(group As List(Of SymbolRecord)) As Boolean
        If group.Count < 2 Then Return False
        Dim projects As HashSet(Of Long) = New HashSet(Of Long)()
        For Each row As SymbolRecord In group
            If Not row.ProjectSymbolId.HasValue Then Return False
            If Not projects.Add(row.ProjectSymbolId.Value) Then Return False
        Next
        Return True
    End Function

    ''' <summary>
    ''' The compiledInto entry a row contributes.
    ''' </summary>
    ''' <param name="row">The row.</param>
    ''' <returns>The entry.</returns>
    Public Function CompiledIntoOf(row As SymbolRecord) As CompiledIntoEnvelope
        Return New CompiledIntoEnvelope With {.ProjectSymbolId = row.ProjectSymbolId, .ProjectName = row.ProjectName, .SymbolId = row.Id, .DocCommentId = row.DocCommentId}
    End Function

    ''' <summary>
    ''' A declaration whose top-level labels are one row's.
    ''' </summary>
    ''' <param name="row">The first twin.</param>
    ''' <param name="compiledInto">Every twin.</param>
    ''' <returns>The declaration.</returns>
    Public Function DeclarationOf(row As SymbolRecord, compiledInto As List(Of CompiledIntoEnvelope)) As DeclarationEnvelope
        Return New DeclarationEnvelope With {
            .SolutionId = row.SolutionId,
            .SolutionKey = row.SolutionKey,
            .Id = row.Id,
            .DocCommentId = row.DocCommentId,
            .Kind = row.Kind,
            .Name = row.Name,
            .Container = NamedRef(row.ContainerId, row.ContainerName),
            .Project = NamedRef(row.ProjectSymbolId, row.ProjectName),
            .Path = row.Path,
            .Line = row.StartLine,
            .CompiledInto = compiledInto}
    End Function

    ''' <summary>
    ''' A container or project reference, or Nothing when the row has none.
    ''' </summary>
    ''' <param name="id">The related row id, or Nothing.</param>
    ''' <param name="name">The related row's name.</param>
    ''' <returns>The reference, or Nothing.</returns>
    Public Function NamedRef(id As Long?, name As String) As NamedRefEnvelope
        If Not id.HasValue Then Return Nothing
        Return New NamedRefEnvelope With {.Id = id.Value, .Name = name}
    End Function

    Private Function KeyOf(row As SymbolRecord) As String
        Return String.Join(vbNullChar, New String() {row.SolutionId.ToString(Globalization.CultureInfo.InvariantCulture), row.Kind, row.Name, row.Path, row.StartLine.ToString(Globalization.CultureInfo.InvariantCulture)})
    End Function

    Private Function EntriesOf(twins As List(Of SymbolRecord)) As List(Of CompiledIntoEnvelope)
        Dim entries As List(Of CompiledIntoEnvelope) = New List(Of CompiledIntoEnvelope)()
        For Each twin As SymbolRecord In twins
            entries.Add(CompiledIntoOf(twin))
        Next
        Return entries
    End Function

    Private Function ById(group As List(Of SymbolRecord)) As List(Of SymbolRecord)
        Dim sorted As List(Of SymbolRecord) = New List(Of SymbolRecord)(group)
        sorted.Sort(Function(a As SymbolRecord, b As SymbolRecord) a.Id.CompareTo(b.Id))
        Return sorted
    End Function

End Module

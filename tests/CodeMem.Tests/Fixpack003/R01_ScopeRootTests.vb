' File: R01_ScopeRootTests.vb
' Project: CodeMem.Tests
' Description: Rule 1 - a declaring file outside the scope root (the repository working directory, else the solution directory) declares nothing; references to it are external targets; rows that leave the scope retire (FR-201..FR-204, FR-207, SC-201, SC-203).
' Author: RCH Automation LLC
' Created: 2026-09-13
'
' RED:   2026-09-13 on the untouched tree, all four red for the named reasons. (a) exit 1: "duplicate doc-comment id T:__FakeTestSdkProgram at
'        ../.nuget/packages/fake.test.sdk/1.0.0/build/net8.0/Fake.Test.Sdk.Program.vb:4,8 and ../.nuget/packages/fake.test.sdk/1.0.0/build/
'        net8.0/Fake.Test.Sdk.Program.vb:4,8" - the relative form and the 4,8 location the 2026-09-12 MemOS refusal printed (STOP 1 ruling).
'        (b) Failure instead of Success: the same refusal at ../../nuget-<guid>/.nuget/packages/... (the repository did not yet scope anything).
'        (c) Assert.DoesNotContain: Project:Sample.Lib is a row. (d) Assert.Equal expected 2 retired, actual 0.
' GREEN: 2026-09-13 after SolutionScope, SourceTrees(scope) and the in-scope project set in ExtractionRun (T006-T009): 4 of 4, plus the
'        duplicate refusal fact, 5 of 5 in 15 s; build 0 warnings / 0 errors.
' FIRE:  2026-09-13 (T011) Resolve used basePath as the root even when repoRoot was present -> (b) red ("the in-repository linked type is not
'        a row"), (a), (c), (d) green; reverted -> green.
' FIRE:  2026-09-13 (T012) Contains compared with StringComparison.Ordinal and without separator normalisation -> the four fixture facts stayed
'        green (the paths they build are already case-consistent and GetFullPath normalises separators on Windows: not fired by them, recorded
'        as such), so fact (e) was added on the door itself; with the same injection (e) red ("case and separators are normalised"); reverted
'        -> green. (e) is not Red-first: written against the finished rule, trusted through this fire.
' 2026-09-15 (feature 004, COR1, T040): fact (f) ContainsDirectoryIncludesTheRootItself added in place - the additive method the bridge's
'        repoPath resolution uses; Contains keeps its file semantics.
' RED:   2026-09-15 (T040) (f) red: the assembly did not compile, ContainsDirectory not a member of SolutionScope (the battery's Red with B05
'        and B06). GREEN: (T041) 6 of 6 once the method landed. FIRE: (T041) ContainsDirectory made prefix-only (the root's own length
'        excluded) -> (f) red ("the root itself"); restored from a byte copy -> green.

Imports System.IO
Imports System.Text.RegularExpressions
Imports CodeMem.Extraction
Imports LibGit2Sharp
Imports Xunit

''' <summary>
''' Four facts on fixture copies: an injected file under a .nuget/packages/ path above the solution (no repository), a repository one level above
''' the solution (the scope root is the repository, not the solution directory), a project file moved outside the scope root, and a re-extraction
''' after a declaring file leaves the scope. The injected file is linked by the relative form the 2026-09-12 refusal printed (STOP 1 ruling).
''' </summary>
<Collection("Fixture")>
Public Class R01_ScopeRootTests

    Private Const InjectedFileName As String = "Fake.Test.Sdk.Program.vb"
    Private Const InjectedRelativeInclude As String = "..\..\.nuget\packages\fake.test.sdk\1.0.0\build\net8.0\" & InjectedFileName
    Private Const InjectedModuleDocId As String = "T:__FakeTestSdkProgram"

    ''' <summary>
    ''' (a) The injected file lives under .nuget/packages/ above the solution, is compiled into both projects by its relative include, and a fixture
    ''' method calls one of its members: the run completes, no symbol or part row carries that path, and the call is an edge with a null target.
    ''' Runs through the executable so the message of a refusal is the recorded stderr line.
    ''' </summary>
    <Fact>
    Public Sub InjectedFileOutsideTheSolutionIsNotASymbol()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                WriteInjected(Path.Combine(copy.ParentDirectory, ".nuget", "packages", "fake.test.sdk", "1.0.0", "build", "net8.0"), True)
                AddCompileItem(copy, "Sample.Lib/Sample.Lib.vbproj", InjectedRelativeInclude)
                AddCompileItem(copy, "Sample.App/Sample.App.vbproj", InjectedRelativeInclude)
                copy.Replace("Sample.Lib/Consumer.vb", "sb.Append(w.ToString())", "sb.Append(w.ToString())" & vbCrLf & "        sb.Append(__FakeTestSdkProgram.Marker())")

                Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(copy.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path))
                Assert.True(run.ExitCode = 0, "exit " & run.ExitCode & ": " & run.StandardError.Trim())

                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim symbols As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
                Dim parts As List(Of PartRow) = MapQueries.ReadParts(map.Path, solutionId)
                Assert.True(symbols.Count > 0, "no symbols written")
                Assert.Empty(symbols.FindAll(Function(s As SymbolRow) s.Path.Contains(".nuget/packages/")))
                Assert.Empty(symbols.FindAll(Function(s As SymbolRow) s.DocCommentId.Contains("__FakeTestSdkProgram")))
                Assert.Empty(parts.FindAll(Function(p As PartRow) p.Path.Contains(".nuget/packages/")))

                Dim calls As List(Of EdgeRow) = MapQueries.ReadEdges(map.Path, solutionId, "calls")
                Dim marker As List(Of EdgeRow) = calls.FindAll(Function(e As EdgeRow) e.SourceDocCommentId = "M:Sample.Consumer.Build" AndAlso e.TargetDocCommentId = "M:__FakeTestSdkProgram.Marker")
                Assert.Single(marker)
                Assert.False(marker(0).TargetSymbolId.HasValue, "an out-of-scope target resolved to a row")
                AssertBalanced(MapQueries.ReadRuns(map.Path)(0))
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (b) A repository whose working directory is the directory above the solution: a file linked from ../Shared inside it is a row (path with a
    ''' parent segment), the injected file outside the repository is not, and solutions.repo_root is the working directory.
    ''' </summary>
    <Fact>
    Public Sub RepositoryAboveTheSolutionIsTheScopeRoot()
        Dim outsideRoot As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "nuget-" & Guid.NewGuid().ToString("N"))
        Try
            Using copy As FixtureCopy = New FixtureCopy()
                Using map As TempMap = New TempMap()
                    Dim injectedDir As String = Path.Combine(outsideRoot, ".nuget", "packages", "fake.test.sdk", "1.0.0", "build", "net8.0")
                    WriteInjected(injectedDir, True)
                    Dim injectedFile As String = Path.Combine(injectedDir, InjectedFileName)
                    AddCompileItem(copy, "Sample.Lib/Sample.Lib.vbproj", Path.GetRelativePath(Path.Combine(copy.Directory, "Sample.Lib"), injectedFile))
                    AddCompileItem(copy, "Sample.App/Sample.App.vbproj", Path.GetRelativePath(Path.Combine(copy.Directory, "Sample.App"), injectedFile))

                    Directory.CreateDirectory(Path.Combine(copy.ParentDirectory, "Shared"))
                    File.WriteAllText(Path.Combine(copy.ParentDirectory, "Shared", "Extra.vb"), "Public Class SharedExtra" & vbCrLf & "End Class" & vbCrLf)
                    AddCompileItem(copy, "Sample.Lib/Sample.Lib.vbproj", "..\..\Shared\Extra.vb")

                    File.WriteAllText(Path.Combine(copy.ParentDirectory, ".gitignore"), "bin/" & vbLf & "obj/" & vbLf)
                    Using repo As Repository = New Repository(Repository.Init(copy.ParentDirectory))
                        Commands.Stage(repo, "*")
                        Dim who As Signature = New Signature("CodeMem.Tests", "tests@codemem.invalid", DateTimeOffset.Now)
                        repo.Commit("fixture copy", who, who)
                    End Using

                    Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}, Nothing))

                    Dim solution As SolutionRow = MapQueries.ReadSolutions(map.Path)(0)
                    Dim symbols As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solution.Id)
                    Dim extra As SymbolRow = symbols.Find(Function(s As SymbolRow) s.DocCommentId = "T:Sample.SharedExtra")
                    Assert.True(extra IsNot Nothing, "the in-repository linked type is not a row")
                    Assert.Equal("../Shared/Extra.vb", extra.Path)
                    Assert.Empty(symbols.FindAll(Function(s As SymbolRow) s.DocCommentId.Contains("__FakeTestSdkProgram")))
                    Assert.True(solution.RepoRoot IsNot Nothing, "repo_root not written")
                    Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(copy.ParentDirectory)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(solution.RepoRoot)), StringComparer.OrdinalIgnoreCase)
                    Dim run As RunRow = MapQueries.ReadRuns(map.Path)(0)
                    Assert.True(run.CommitSha IsNot Nothing, "commit_sha not written")
                    AssertBalanced(run)
                End Using
            End Using
        Finally
            If Directory.Exists(outsideRoot) Then Directory.Delete(outsideRoot, True)
        End Try
    End Sub

    ''' <summary>
    ''' (c) Sample.Lib moved to a directory beside the solution (no repository, so the scope root is the solution directory): no project row, none of
    ''' its types, a depends_on and an extends edge to it with null targets, and the shared namespace keeps only its Sample.App part.
    ''' </summary>
    <Fact>
    Public Sub ProjectFileOutsideTheScopeRootContributesNothing()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim outside As String = Path.Combine(copy.ParentDirectory, "Outside")
                Directory.CreateDirectory(outside)
                Directory.Move(Path.Combine(copy.Directory, "Sample.Lib"), Path.Combine(outside, "Sample.Lib"))
                copy.Replace("Sample.sln", """Sample.Lib\Sample.Lib.vbproj""", """..\Outside\Sample.Lib\Sample.Lib.vbproj""")
                copy.Replace("Sample.App/Sample.App.vbproj", "..\Sample.Lib\Sample.Lib.vbproj", "..\..\Outside\Sample.Lib\Sample.Lib.vbproj")
                DotnetCli.Run("restore """ & copy.SolutionPath & """", copy.Directory)

                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}, Nothing))

                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim symbols As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
                Assert.Contains(symbols, Function(s As SymbolRow) s.DocCommentId = "Project:Sample.App")
                Assert.DoesNotContain(symbols, Function(s As SymbolRow) s.DocCommentId = "Project:Sample.Lib")
                Assert.DoesNotContain(symbols, Function(s As SymbolRow) s.DocCommentId = "T:Sample.Widgets.LeafWidget")

                Dim edges As List(Of EdgeRow) = MapQueries.ReadEdges(map.Path, solutionId, Nothing)
                Dim dependsOn As EdgeRow = edges.Find(Function(e As EdgeRow) e.Verb = "depends_on" AndAlso e.SourceDocCommentId = "Project:Sample.App" AndAlso e.TargetDocCommentId = "Project:Sample.Lib")
                Assert.True(dependsOn IsNot Nothing, "no depends_on edge to the out-of-scope project")
                Assert.False(dependsOn.TargetSymbolId.HasValue, "depends_on to an out-of-scope project resolved to a row")
                Dim extends As EdgeRow = edges.Find(Function(e As EdgeRow) e.Verb = "extends" AndAlso e.SourceDocCommentId = "T:Sample.Widgets.AppWidget" AndAlso e.TargetDocCommentId = "T:Sample.Widgets.LeafWidget")
                Assert.True(extends IsNot Nothing, "no extends edge to the out-of-scope base type")
                Assert.False(extends.TargetSymbolId.HasValue, "extends to an out-of-scope type resolved to a row")

                Dim widgets As SymbolRow = symbols.Find(Function(s As SymbolRow) s.DocCommentId = "N:Sample.Widgets")
                Assert.True(widgets IsNot Nothing, "the namespace declared by both projects is missing")
                Dim widgetParts As List(Of PartRow) = MapQueries.ReadParts(map.Path, solutionId).FindAll(Function(p As PartRow) p.SymbolId = widgets.Id)
                Assert.True(widgetParts.Count > 0, "the namespace has no parts")
                For Each part As PartRow In widgetParts
                    Assert.StartsWith("Sample.App/", part.Path)
                Next
                AssertBalanced(MapQueries.ReadRuns(map.Path)(0))
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (d) Run 1 with the injected file inside Sample.Lib (in scope); the file then moves under .nuget/packages/ above the solution and is linked
    ''' back by its relative include: run 2 retires the module and its Main (rows kept, inactive), writes no candidate, matches everything else.
    ''' </summary>
    <Fact>
    Public Sub DeclarationsThatLeaveTheScopeAreRetired()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim generated As String = Path.Combine(copy.Directory, "Sample.Lib", "Generated")
                WriteInjected(generated, False)
                Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim injectedRows As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId).FindAll(Function(s As SymbolRow) s.DocCommentId.Contains("__FakeTestSdkProgram"))
                Assert.Equal(2, injectedRows.Count)

                Dim injectedDir As String = Path.Combine(copy.ParentDirectory, ".nuget", "packages", "fake.test.sdk", "1.0.0", "build", "net8.0")
                Directory.CreateDirectory(injectedDir)
                File.Move(Path.Combine(generated, InjectedFileName), Path.Combine(injectedDir, InjectedFileName))
                Directory.Delete(generated)
                AddCompileItem(copy, "Sample.Lib/Sample.Lib.vbproj", InjectedRelativeInclude)

                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
                Dim run1 As RunRow = runs(0)
                Dim run2 As RunRow = runs(1)
                Assert.Equal(2, run2.SymbolsRetired)
                Assert.Equal(0, run2.RenameCandidates)
                Assert.Equal(0, run2.SymbolsNew)
                Assert.Equal(run1.SymbolsObserved - 2, run2.SymbolsMatched)
                Dim symbolsAfter As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
                For Each row As SymbolRow In injectedRows
                    Dim kept As SymbolRow = symbolsAfter.Find(Function(s As SymbolRow) s.Id = row.Id)
                    Assert.True(kept IsNot Nothing, "a retired row was deleted: " & row.DocCommentId)
                    Assert.False(kept.IsActive, "an out-of-scope row stayed active: " & row.DocCommentId)
                Next
                Assert.Equal(0L, MapQueries.CountRows(map.Path, "rename_candidates", solutionId))
                AssertBalanced(run2)
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (e) The comparison the STOP 1 ruling added, on the door itself: relative segments resolved, separators normalised, case-insensitive; the
    ''' trailing separator keeps a sibling directory with the same prefix out; without a repository the root is the solution directory.
    ''' </summary>
    <Fact>
    Public Sub PrefixComparisonIsResolvedCaseInsensitiveAndSeparatorNormalised()
        Dim root As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "Scope-" & Guid.NewGuid().ToString("N"))
        Dim scope As SolutionScope = SolutionScope.Resolve(root, Path.Combine(root, "Solution"))
        Assert.True(scope.IsRepository)
        Assert.EndsWith(Path.DirectorySeparatorChar.ToString(), scope.Root)
        Assert.True(scope.Contains(Path.Combine(root, "Sub", "File.vb")), "a file under the root")
        Assert.True(scope.Contains(Path.Combine(root, "Sub", "..", "Other", "File.vb")), "relative segments are resolved")
        Assert.True(scope.Contains(root.ToUpperInvariant() & "/SUB/FILE.VB"), "case and separators are normalised")
        Assert.False(scope.Contains(root & "2" & Path.DirectorySeparatorChar & "File.vb"), "a sibling directory sharing the prefix")
        Assert.False(scope.Contains(Path.Combine(root, "..", "Elsewhere", "File.vb")), "a file above the root")
        Dim fallback As SolutionScope = SolutionScope.Resolve(Nothing, Path.Combine(root, "Solution"))
        Assert.False(fallback.IsRepository)
        Assert.True(fallback.Contains(Path.Combine(root, "Solution", "A.vb")), "inside the solution directory")
        Assert.False(fallback.Contains(Path.Combine(root, "Shared", "A.vb")), "beside the solution directory, no repository")
    End Sub

    ''' <summary>
    ''' (f) Feature 004 (COR1, T040): ContainsDirectory answers "is this directory the root or under it" - the root itself with and without its
    ''' trailing separator, a subdirectory, mixed case and forward slashes True; the parent and a same-prefix sibling False; Contains keeps its
    ''' file semantics unchanged.
    ''' </summary>
    <Fact>
    Public Sub ContainsDirectoryIncludesTheRootItself()
        Dim root As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "Scope-" & Guid.NewGuid().ToString("N"))
        Dim scope As SolutionScope = SolutionScope.Resolve(root, root)
        Assert.True(scope.ContainsDirectory(root), "the root itself")
        Assert.True(scope.ContainsDirectory(root & Path.DirectorySeparatorChar), "the root with its trailing separator")
        Assert.True(scope.ContainsDirectory(Path.Combine(root, "sub")), "a subdirectory")
        Assert.True(scope.ContainsDirectory(root.ToUpperInvariant() & "/SUB/DEEPER"), "case and separators are normalised")
        Assert.False(scope.ContainsDirectory(Path.GetDirectoryName(root)), "the parent")
        Assert.False(scope.ContainsDirectory(root & "2"), "a same-prefix sibling")
        Assert.True(scope.Contains(Path.Combine(root, "sub", "File.vb")), "Contains: a file under the root")
        Assert.False(scope.Contains(root & "2" & Path.DirectorySeparatorChar & "File.vb"), "Contains: a sibling directory sharing the prefix")
    End Sub

    ''' <summary>
    ''' Writes the stand-in for the test SDK's injected program: a Global-namespace module with a Main at line 4, column 8, so a refusal names the
    ''' same location the 2026-09-12 refusal did; optionally a public function a fixture method can call.
    ''' </summary>
    ''' <param name="directory">Where the file goes; created if absent.</param>
    ''' <param name="withMarker">Whether to add the callable function.</param>
    Private Shared Sub WriteInjected(directory As String, withMarker As Boolean)
        IO.Directory.CreateDirectory(directory)
        Dim lines As List(Of String) = New List(Of String) From {
            "' <auto-generated> This file stands in for the test SDK's injected program. </auto-generated>",
            "Imports System",
            "Namespace Global",
            "Module __FakeTestSdkProgram",
            "Sub Main(args As String())",
            "End Sub"}
        If withMarker Then lines.AddRange(New String() {"Public Function Marker() As Integer", "Return 1", "End Function"})
        lines.AddRange(New String() {"End Module", "End Namespace"})
        File.WriteAllText(Path.Combine(directory, InjectedFileName), String.Join(vbCrLf, lines) & vbCrLf)
    End Sub

    ''' <summary>
    ''' Appends one Compile item to a project file of the copy.
    ''' </summary>
    ''' <param name="copy">The fixture copy.</param>
    ''' <param name="projectFile">Project file path relative to the copy, forward slashes.</param>
    ''' <param name="include">The Include value (a relative path from the project directory).</param>
    Private Shared Sub AddCompileItem(copy As FixtureCopy, projectFile As String, include As String)
        copy.Replace(projectFile, "</Project>", "  <ItemGroup>" & vbCrLf & "    <Compile Include=""" & include & """ />" & vbCrLf & "  </ItemGroup>" & vbCrLf & vbCrLf & "</Project>")
    End Sub

    Private Shared Sub AssertBalanced(run As RunRow)
        Assert.Equal("completed", run.Outcome)
        Assert.Equal(0, run.UnaccountedObserved)
        Assert.Equal(0, run.UnaccountedRegistry)
    End Sub

End Class

# Research: CodeMem Stage A — Extractor and codemem.sqlite

**Feature**: `001-extractor-codemem-sqlite` | **Date**: 2026-09-09 | **Spec**: [spec.md](spec.md)

Every decision below was either verified empirically on this machine (marked **Verified**) or is a
reasoned choice among alternatives (marked **Decided**). Nothing here is a guess presented as fact.
Probe projects live in the session scratchpad and are not part of the repository.

## Environment facts (Verified)

| Fact | Evidence |
|------|----------|
| SDKs installed: 9.0.308, 10.0.303. **No 8.x SDK.** | `dotnet --list-sdks` |
| Runtimes installed include Microsoft.NETCore.App 8.0.31 and Microsoft.WindowsDesktop.App 8.0.31 | `dotnet --list-runtimes` |
| A net8.0 VB project builds and runs under SDK 10 | XmlDocProbe, WsProbe |
| The VB compiler emits **no diagnostic** for a missing XML doc comment even with `GenerateDocumentationFile` on and `TreatWarningsAsErrors` | Constitution amendment v1.1.0 record; C# control emitted CS1591 |

## R1. Roslyn package line (Verified)

**Decision**: `Microsoft.CodeAnalysis.VisualBasic.Workspaces` **4.14.0** and
`Microsoft.CodeAnalysis.Workspaces.MSBuild` **4.14.0**. No `Microsoft.Build.Locator`.

**Rationale**: Roslyn 5.9.0 (the latest) ships `Workspaces.MSBuild` for `net10.0` and `net472` only;
`MSBuildWorkspace` does not exist in its net8.0 dependency graph (build error BC30002, confirmed).
4.14.0 is the last 4.x line, builds clean on net8.0, and its out-of-process build host located SDK 10
without `MSBuildLocator`: the probe opened a project, obtained a compilation with 163 metadata
references and zero errors, on runtime 8.0.31.

**Alternatives considered**: Roslyn 5.x on net10.0 — rejected, Article XV fixes .NET 8.
`Microsoft.Build.Locator` 1.11.2 — not needed; kept as the documented fallback if the build host
fails to find an SDK on another machine (it would be added as a dependency, not a runtime).

## R2. Declaring-reference shape in VB (Verified — load-bearing for hashing)

Probe output for a class with one method:

```text
NamedType Undocumented   ref kind=ClassStatement    span=[0..109)   tokens=3
Method    NoDocs         ref kind=FunctionStatement span=[30..65)   tokens=7
Namespace XmlDocProbe    ref kind=CompilationUnit ×3 (one per document, incl. 2 generated)
Method    .ctor          implicit=True  declRefs=0
```

Three findings, each with a rule:

1. **`GetSyntax()` returns the begin statement, not the block.** The type's reference span covers the
   whole block; the method's covers only the statement. Hashing the reference node would hash
   `Public Function NoDocs() As Integer` and none of the body.
   **Rule (Part node promotion)**: `node = r.GetSyntax()`; if `node.Parent` is a block whose begin
   statement is `node` (`TypeBlockSyntax`, `MethodBlockBaseSyntax`, `PropertyBlockSyntax`,
   `EventBlockSyntax`, `EnumBlockSyntax`, `NamespaceBlockSyntax`), the part node is `node.Parent`.
   Otherwise (auto-property, `MustOverride`, `Declare`, field declarator, enum member) it is `node`.
   The part's span is the **part node's** span, never `r.Span`.
2. **The root namespace has a declaring reference per compilation unit** even with no `Namespace`
   statement. Taken literally it would get a `body_hash` over every file in the project.
   **Rule**: a namespace's parts are its `NamespaceBlockSyntax` references only; references whose node
   is a `CompilationUnitSyntax` are ignored. A namespace with zero such parts is not written (matches
   the spec edge case: root and global namespaces are not rows).
3. **Implicit constructor**: `IsImplicitlyDeclared = True`, zero declaring references, but it *does*
   have a doc-comment id (`M:…#ctor`). Confirms spec Q4: skip it as a row; redirect a `calls` edge whose
   target is an implicit constructor to the containing type's doc id.

## R3. Generated documents (Verified)

`Project.Documents` includes `obj/Debug/net8.0/<Project>.AssemblyInfo.vb` and
`obj/…/.NETCoreApp,Version=v8.0.AssemblyAttributes.vb`. With SourceLink enabled by default for git
repositories, `AssemblyInfo.vb` embeds the commit sha in `AssemblyInformationalVersion`, so including
it would change the source digest on every commit even when no source changed.

**Decision**: the compiled-inputs enumeration (FR-005) is `Project.Documents` **excluding documents
whose path lies under the project's intermediate output directory** (`obj/`), plus each `.vbproj`, plus
the solution file. The exclusion is a deterministic path rule keyed on the project's
`IntermediateOutputPath`, not a heuristic. Generated documents declare no symbols, so nothing else
changes.

## R4. Hash algorithm and token serialization (Decided)

**Decision**: SHA-256 from `System.Security.Cryptography` (built in, no dependency), stored as
64-character lowercase hex `TEXT`. Input for a part is the sequence of `SyntaxToken.Text` values of
the part node's `DescendantTokens()`, each followed by LF (`&H0A`), with the symbol's own identifier
tokens omitted (spec Q2). `body_hash` = SHA-256 over the concatenation of the parts' *hash inputs*
in (path ordinal, start_offset) order, separated by a NUL byte.

**Own-identifier tokens by kind**: `TypeStatementSyntax.Identifier`; `DelegateStatementSyntax.Identifier`;
`MethodStatementSyntax.Identifier` (constructors have none); `PropertyStatementSyntax.Identifier`;
`EventStatementSyntax.Identifier`; `EnumMemberDeclarationSyntax.Identifier`; all tokens of
`NamespaceStatementSyntax.Name`; for a field, `ModifiedIdentifierSyntax.Identifier`.

**Field parts and sibling declarators**: a field's declaring reference is its
`ModifiedIdentifierSyntax` (one token). The part node is the enclosing `VariableDeclaratorSyntax`, so
the `As` clause and initializer are hashed. For a declaration statement with several declarators
(`Dim a, b As Integer`), **every declarator's identifier token is excluded from every sibling's
hash**, not only the symbol's own — renaming `b` must not change `a`'s identity (spec FR-013). The
exclusion set for a field is therefore all `ModifiedIdentifierSyntax.Identifier` tokens of its
`VariableDeclaratorSyntax`.

**Alternatives**: xxHash/FNV (extra dependency or hand-rolled; no benefit — throughput is not a
goal); `ToFullString()` text (rejected by Q2 — trivia must not affect identity).

## R5. Source digest (Decided)

SHA-256 over, for each compiled input in `StringComparer.Ordinal` order of solution-relative path:
UTF-8 bytes of the path, `&H00`, UTF-8 bytes of the text with CRLF→LF and a leading BOM removed,
`&H00`. Paths use `/`. For a `.vbproj` input the base directory is the project directory.

## R6. Lock and atomic publication (Decided)

**Decision**: the lock *is* the SQLite write transaction. `journal_mode = DELETE` (single file, no
`-wal`/`-shm` sidecars — honest to Article IX's "one file"), `foreign_keys = ON`,
`SqliteConnection.DefaultTimeout = 0`, and `BEGIN IMMEDIATE` taken before extraction. A second
extractor's `BEGIN IMMEDIATE` fails at once with `SQLITE_BUSY` → exit 3, no waiting (SC-008). Readers
keep reading the previous snapshot under a RESERVED lock and are blocked only for the duration of the
final commit. Staging is in memory (spec Q5), so nothing touches published tables before validation;
on residual failure the failed `extract_runs` row is inserted and committed inside the same
transaction; on success the whole publication commits at once; any abort rolls back (I9).

**Alternatives**: a sidecar `.lock` file with `FileShare.None` — works, but is a second mechanism
beside the transaction and leaves a stale file on some crash paths; WAL mode — better concurrent
reads, but two sidecar files that a naive copy of `codemem.sqlite` would miss. WAL can be revisited
when the MCP server exists; nothing in the schema depends on journal mode.

**Must be verified by test, not assumed**: that `DefaultTimeout = 0` yields immediate `SQLITE_BUSY`
under Microsoft.Data.Sqlite (I10 is that test).

## R7. Data access (Decided)

`Microsoft.Data.Sqlite` **8.0.31** — same major as the runtime; bundles `e_sqlite3` through
SQLitePCLRaw. Article XI: used directly, all SQL in named repository methods, parameters named and
bound. 10.0.12 also targets net8.0 via netstandard2.0 but adds nothing needed.

## R8. Git provenance (Decided)

`LibGit2Sharp` **0.32.0**. `Repository.Discover(solutionDirectory)` → null when absent (sha and flag
null). Sha = `repo.Head.Tip.Sha`. Dirty flag = any compiled input (R3 enumeration) whose
`repo.RetrieveStatus(relativePath)` is not `Unaltered`/`Ignored` — modified, staged, or untracked
(spec Q3). `repo_root` label = `repo.Info.WorkingDirectory`. Never shells out.

## R9. Test framework (Decided)

`xunit` **2.9.3**, `xunit.runner.visualstudio` **3.1.5**, `Microsoft.NET.Test.Sdk` **17.14.1**,
`Xunit.SkippableFact` for the runtime-Skip acceptance runner (version pinned in plan.md after
lookup). xUnit's `ICollectionFixture(Of T)` is literally "loaded once per test collection, shared
read-only" (FR-036), and tests in one collection run sequentially, which is what a shared
compilation wants. xunit v3 (`xunit.v3`) has built-in `Assert.Skip` but is a different package line
with a different runner model; not worth the churn for one attribute.

## R10. Fixture loading (Decided)

`MSBuildWorkspace` performs a design-time build; it needs the fixture **restored** (an
`obj/project.assets.json`) but not built. The collection fixture runs `dotnet restore` on the fixture
once before loading — a dev-time invocation of the platform SDK, permitted by Article XV. Mutation
tests (I1, I4, I6, I7) copy the fixture directory (excluding `bin/`, `obj/`) to a temp directory,
restore, mutate, load. The WinForms project targets `net8.0-windows`; `Microsoft.WindowsDesktop.App`
8.0.31 is installed.

## R11. Test seams (Decided)

Two seams, both reachable through the production entry point (Article XIII):

- **Count corruption (I8)**: `ExtractionRun.Execute(options, seams)` accepts an optional `RunSeams`
  whose `CorruptStagedCounts` delegate is invoked on the staged `RunCounts` before the auditor runs.
  `Main` passes `Nothing`. Tests call `Execute` — the same method `Main` calls.
- **Process abort (I9)**: environment variable `CODEMEM_TEST_ABORT_AT` with values `AfterStaging` or
  `DuringPublish`; when set, the orchestrator calls `Environment.FailFast` at that point. A real process
  abort, driven through the real executable. Labeled test-only in `--help` output per the "no unwired
  surface presented as real" rule.

## R12. Extractor version and schema version (Decided)

`extractor_version` = `AssemblyName.Version` of the Extractor assembly (from `<Version>` in the
project file), formatted `Major.Minor.Build`. Not `InformationalVersion`, which SourceLink suffixes
with the commit sha and would vary per commit for one binary. `schema_version` = a constant in Core
(`SchemaVersion.Current = 1`), written to `map_identity` at creation and to every run; mismatch on
open → exit 1.

## R13. Symbol-binding conventions (Decided)

- Every edge target is `symbol.OriginalDefinition` (generic instantiations bind to the definition);
  reduced extension methods use `ReducedFrom`.
- `calls` source attribution: `SemanticModel.GetEnclosingSymbol(position)` then walk
  `ContainingSymbol` until a symbol that has a row (accessor → property; lambda → enclosing method;
  field initializer → field) — FR-016.
- `uses` on a composite type syntax (`List(Of Foo)`, `Foo()`) yields one edge per **named** type
  occurrence, each with its own span: `List`1` and `Foo`. `Integer?` names only `Integer`.
- `part_of` target = `ContainingSymbol.OriginalDefinition`; no edge when the container is the global
  namespace (nothing to name); doc-id-only edge when the container is the root namespace (R2).
- `depends_on` occurrence span = the first occurrence of the referenced project file's name in the
  referencing `.vbproj` text (Roslyn's `ProjectReference` carries no location). If absent (reference
  injected by a props file), offset 0 / line 1 — a known I12 limit outside the fixture.
- `handles` from a `Handles` clause: one edge per `HandlesClauseItemSyntax`; target = the bound event's
  `OriginalDefinition`; `via_symbol_id` = the bound `WithEvents` member's row when source-declared;
  span = the event name token. From `AddHandler`: source = the bound method of the delegate operand
  (`AddressOf` expression); target = the bound event of the event operand; skipped when either does
  not bind to a method/event.

## R14. Project rows (Decided)

Zero parts, so `body_hash` = SHA-256 of the empty input. Consequence: a project retired and another
minted in the same run always satisfy (B) (same kind `project`, null container, equal hash) and
produce a candidate — a renamed project file is detected for free; two renamed in one run rank by
proximity, which for two project files in different paths is "different path, rank by id".
Deterministic; recorded.

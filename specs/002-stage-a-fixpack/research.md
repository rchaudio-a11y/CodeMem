# Research: CodeMem Fixpack 002 — Stage A Review Corrections

**Feature**: `002-stage-a-fixpack` | **Date**: 2026-09-10 | **Spec**: [spec.md](spec.md)

Every item below was verified on this machine with a scratch probe (marked **Verified**) or is a reasoned
choice among alternatives (marked **Decided**). Probe projects live in the session scratchpad and are not
part of the repository. Numbering continues Stage A's research (R1–R14) at R21.

## Environment facts (Verified, 2026-09-10)

| Fact | Evidence |
|------|----------|
| SDKs installed: 9.0.308, **10.0.401** (10.0.303 at Stage A time has been superseded) | resolver probe, `dotnet --list-sdks` |
| Host: `C:\Program Files\dotnet\host\fxr\10.0.12\hostfxr.dll` is loaded in every .NET process on this machine, including the xUnit test host | `Process.GetCurrentProcess().Modules` |
| Runtime 8.0.31 still runs the net8.0 projects | Stage A build unchanged |

## R21. `sdk_version`: the resolved SDK, read in-process (Verified — spec Clarifications Q1)

**Decision**: call the host's own SDK resolver, `hostfxr_resolve_sdk2`, through P/Invoke on the `hostfxr`
module already loaded in the process, with `exe_dir` = the dotnet root (three directories above
`hostfxr.dll`) and `working_dir` = the solution's base directory. The result callback's key 0 is the
resolved SDK directory; its leaf name is the version (`10.0.401`). This is exactly the resolution
`dotnet --version` performs in that directory, `global.json` included, without launching a process.

**Evidence** (probe output, abridged):

```text
hostfxr=C:\Program Files\dotnet\host\fxr\10.0.12\hostfxr.dll
hostfxr_set_error_writer export=True
--- no global.json                       key 0 = C:\Program Files\dotnet\sdk\10.0.401   key 3 = not_found   rc=0
--- global.json 9.0.100 latestPatch      key 2 = 9.0.100  key 3 = valid   rc=-2147450725 (no 9.0.1xx installed)
--- global.json 7.0.100 disable          rc=-2147450725
--- global.json 9.0.100 latestFeature    key 0 = C:\Program Files\dotnet\sdk\9.0.308   rc=0
```

Result keys: 0 = resolved SDK directory, 1 = `global.json` path, 2 = requested version, 3 = `global.json`
state (`valid` / `not_found` / `invalid`). Return 0 on success; `0x8000809B` when no compatible SDK exists.

**Error text goes through the host's error writer, not stderr**: on failure the host prints several
lines ("A compatible .NET SDK was not found. … Install the [9.0.100] .NET SDK …"). The probe confirmed
`hostfxr_set_error_writer` exists and captures every line into a buffer; the extractor installs a writer
before resolving and folds the captured text into the single stderr line of the exit-1 refusal (FR-115).

**Locating the host**: prefer the `hostfxr.dll` module of the current process; if absent (a host that
did not load it — not observed), fall back to `DOTNET_ROOT`, then the directory of `dotnet` on `PATH`,
taking the highest `host/fxr/<version>/hostfxr.dll`. No SDK found or no host found → `SdkResolutionException`
→ exit 1 (spec Assumptions: never a substitute value).

**Why this equals what compiled**: Roslyn's out-of-process build host locates the SDK with the same host
API and the same working directory (its SDK location helper calls `hostfxr_resolve_sdk2` for the
directory of the project being loaded), so the stamp and the build host agree by construction. Verified
here only that the resolver honours `global.json`; the build host's use of the same API is Roslyn's
documented behaviour, not re-probed.

**Alternatives rejected**: `RuntimeInformation.FrameworkDescription` (names the runtime, `.NET 8.0.31`,
not the SDK); running `dotnet --version` (a process launch; Article XV allows dev-time SDK use but the
spec forbids launching a process for this value); parsing `global.json` and enumerating `sdk/`
ourselves (re-implementing roll-forward rules the host already owns).

## R22. One schema shape: "create v1, then migrate" (Verified — spec Clarifications Q2)

**Decision**: `contracts/schema.sql` is the version-1 DDL followed by the 1→2 migration
(`ALTER TABLE extract_runs ADD COLUMN sdk_version TEXT NULL`, two triggers, `UPDATE map_identity SET
schema_version = 2`). A fresh map runs the v1 DDL, inserts its identity row at version 1, then runs the
same migration statements a v1 map runs. Both paths execute byte-identical statements, so
`sqlite_master` (`type, name, tbl_name, sql`) is identical by construction — no hand-replication of the
text SQLite produces.

**Evidence**: after `ALTER TABLE … ADD COLUMN sdk_version TEXT NULL`, SQLite rewrites the stored `sql` in
place, inserting `, sdk_version TEXT NULL` after the last column definition and before any trailing
constraint text:

```text
CREATE TABLE extract_runs (\n    id INTEGER PRIMARY KEY AUTOINCREMENT,\n    schema_version INTEGER NOT NULL, sdk_version TEXT NULL,   -- comment\n    CHECK (...)\n)
```

Writing a fresh CREATE TABLE that matches that text exactly would be fragile; running the same ALTER on
fresh maps is not. `sqlite_sequence` exists on both paths. Comparison in the test: the four columns above,
ordered by (`type`, `name`); `rootpage` is excluded (allocation order differs).

**Trigger** (fires as expected; message surfaces through `SqliteException`):

```text
trigger fired: SQLite Error 19: 'sdk_version is required for schema_version >= 2'.
v1 row without sdk_version accepted
```

Two triggers: `BEFORE INSERT` and `BEFORE UPDATE OF sdk_version, schema_version`, both `WHEN
NEW.schema_version >= 2 AND NEW.sdk_version IS NULL`. Named `tr_<table>_<purpose>` (the constitution names
no trigger convention; this mirrors `ix_`).

**Inside the write transaction**: `ALTER TABLE`, `CREATE TRIGGER` and the version update are all
transactional in SQLite, so an abort rolls the map back to version 1 intact (FR-113). `PRAGMA
journal_mode` and `PRAGMA foreign_keys` are not transactional and are executed at open, before
`BEGIN IMMEDIATE`; they leave the schema file as documentation only.

## R23. Fresh-map detection and the foreign-file refusal (Verified)

| Input file | Observation | Classification |
|------------|-------------|----------------|
| absent | — | fresh |
| 0 bytes | `sqlite_master` user-table count = 0 | fresh |
| SQLite file, user tables = 0 (rolled-back first initialization) | count = 0 | fresh |
| SQLite file with tables, no `map_identity` | count > 0, table missing | **not a map** → exit 1, nothing written |
| `map_identity` present, no row | row missing | not a map → exit 1 |
| random bytes | first statement fails: `SqliteErrorCode=26 'file is not a database'` | exit 1 |

Detection query: `SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'`,
issued after `BEGIN IMMEDIATE` so the classification and the creation happen under one lock.

**Consequence for refused runs**: creation now happens inside the transaction; a refusal (compile
errors, exit 2) rolls it back and leaves an empty file, which is fresh next time. Stage A's assumption
"a refused run may leave a freshly created map file" becomes "may leave an empty file".

## R24. Connection string and path failures (Verified)

- `SqliteConnectionStringBuilder With {.DataSource = path, .Pooling = False}` renders
  `Data Source="…\sdk;probe.sqlite";Pooling=False` and opens exactly that file (F5).
- `Path.GetFullPath(" ")` throws `ArgumentException`; `GetFullPath` accepts `<`, `>` and `|` on .NET 8, and
  the open then fails with `SqliteErrorCode=14 'unable to open database file'`.
- **Exit-code correction**: Stage A mapped SQLite 14 (`CANTOPEN`) to exit 3 alongside 8 (`READONLY`).
  An illegal character is a usage-class failure, so 14 maps to exit 1; 8 (a read-only medium at
  `BEGIN IMMEDIATE`) stays exit 3. I10 and the new preflight test cover both.

## R25. Vulnerable transitives: the lowest clearing versions (Verified)

| Package | Resolved at Stage A | Advisory | Lowest patched at or above | Result |
|---------|--------------------:|----------|---------------------------:|--------|
| Microsoft.Build.Tasks.Core | 17.7.2 | GHSA-h4j7-5rxr-p4wc (CVE-2025-26646) | 17.8.29 | clears it, **but surfaces GHSA-w3q9-fxm7-j8fq** (CVE-2025-55247) on Tasks.Core and Utilities.Core 17.8.29 |
| Microsoft.Build.Tasks.Core | — | GHSA-w3q9-fxm7-j8fq | **17.8.43** | audit clean |
| System.Formats.Asn1 | 7.0.0 | GHSA-447r-wph3-92pm (CVE-2024-38095) | **8.0.1** (6.0.1 is below the resolved 7.0.0) | audit clean |

**Decision**: pin `Microsoft.Build.Tasks.Core` 17.8.43 and `System.Formats.Asn1` 8.0.1 in
`CodeMem.Extraction.vbproj`. "Lowest version that clears the named advisory" is read as "lowest that
leaves the audit with no High advisory", because 17.8.29 trades one High for another. Scratch audit with
Roslyn 4.14.0 + LibGit2Sharp 0.32.0 + Microsoft.Data.Sqlite 8.0.31 + the pins:

```text
The given project `PinProbe` has no vulnerable packages given the current sources.
   > Microsoft.Build.Tasks.Core       17.8.43
   > Microsoft.Build.Framework        17.8.43   (transitive, lifted by the pin)
   > Microsoft.Build.Utilities.Core   17.8.43   (transitive, lifted by the pin)
   > Microsoft.Build                  17.7.2    (transitive, not flagged by either advisory)
   > System.Formats.Asn1              8.0.1
```

Build: 0 warnings, 0 errors.

**FR-119 finding, recorded now**: the out-of-process build host folder (`BuildHost-netcore/`) ships only
`Microsoft.Build.Locator.dll`; it loads MSBuild from the installed SDK (10.0.401 here), not from the
package graph. The pins therefore change what the extractor process ships and loads, and cannot change
what the build host loads — which is the SDK's own, patched MSBuild. This is the finding the spec asked
to record rather than force; the quickstart carries it.

## R26. Abort seam nonce (Decided — no new CLI argument)

`CODEMEM_TEST_ABORT_AT` = `<phase>:<nonce>` and `CODEMEM_TEST_NONCE` = `<nonce>`. The seam is armed only
when both are set, the phase is a known `RunPhase`, both nonce texts are non-empty, and they are equal
by ordinal comparison. Either variable alone, a phase without a nonce, or unequal nonces → inert. A test
mints a fresh GUID per run and sets both. The in-process `RunSeams` object is unchanged (it is passed,
not inherited). `--help` documents both under the test-only heading.

Rejected: a `--test-nonce` argument (the spec forbids new CLI arguments); a file-based token (a second
channel to document and clean up).

## R27. Kind refresh and the kind-change fixture edit (Decided)

`RefreshMatched` and `Reactivate` gain `kind = @kind`; identity (`solution_id`, `doc_comment_id`) is
untouched, so the reconciler is unchanged and (B) still compares kinds. Fixture edit: `Fields.vb`
declares one class with two private fields and one function, nothing instantiates it, and both `Public
Class Fields` and `End Class` occur exactly once, so `FixtureCopy.Replace` can take it Class → Structure
→ Module across three runs; `F:Sample.Fields.a` and `M:Sample.Fields.Total` keep their doc ids in all
three kinds (a Module's members are Shared; doc ids do not encode that).

## R28. Build files in the compiled inputs (Decided)

Walk from the solution base directory through `DirectoryInfo.Parent` until null; at each level add
`Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json` when the
file exists (Windows file lookup is case-insensitive). Each joins the existing enumeration with the same
normalisation and ordinal ordering, under its solution-relative path (`../Directory.Build.props`).
Git status: files above the repository root are outside the working directory and are skipped by the
dirty check exactly as Stage A already skips `../` inputs; files inside the repository are checked.

**Test isolation**: `FixtureCopy` now copies to `<temp>/codemem-tests/fixture-<guid>/Sample/` and
exposes `ParentDirectory` (`fixture-<guid>`), so a test can drop a props file "above the copy" without
touching any other test's copy. The repository itself has no build files above the committed fixture;
the digest of the committed fixture changes only if the developer machine has one above the repository,
which is precisely the provenance the feature adds.

## R29. Gate normalisation (Decided)

Both gates normalise scanned literals with `\s+` → one space and match with `RegexOptions.IgnoreCase`;
the tripwire's positive-count patterns (`INSERT INTO`, `UPDATE `) are normalised the same way so the
vacuous-Red guard stays meaningful. Fire injections: `delete   from code_symbols where id = @id`
(repository) and `select 1` (non-repository).

## R30. The version-1 map fixture (Decided)

A binary fixture `tests/CodeMem.Tests/Fixtures/Maps/sample-v1.sqlite` is produced **before any code
change** by the committed Stage A extractor (commit `3ee22c6`) from the committed fixture solution, so
the upgrade test runs against a map the Stage A code actually wrote. Tests copy it to a temp path before
opening it. `C:\_DB\codemem.sqlite` is validated in the quickstart against a copy first; the original is
upgraded by the Operator's next real run.

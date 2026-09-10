# Version-1 map fixture

`sample-v1.sqlite` was produced on 2026-09-10 by the **Stage A extractor as committed at `63d62fa`**
(code unchanged since `a0924fe`), before any fixpack 002 source change (research R30), by running

```powershell
dotnet run --project src/CodeMem.Extractor -- --solution tests/CodeMem.Tests/Fixtures/Sample/Sample.sln --db tests/CodeMem.Tests/Fixtures/Maps/sample-v1.sqlite
```

twice, so the registry holds matched rows and two completed runs. No `-journal` file remained.
Tests never open the committed file; `V1MapFixture.CopyToTemp()` copies it (from the test output
directory, where the project file copies it) to a fresh `TempMap` path first.

Observed contents at creation:

```text
map_identity.schema_version = 1

table              rows
map_identity          1
solutions             1
extract_runs          2     (id 1 and 2, both completed, schema_version 1, is_dirty 0)
code_symbols         38
code_parts           39
code_edges          137
rename_candidates     0
sqlite_sequence       5

run 1: observed=38 matched=0  new=38 retired=0 registry_before=0
run 2: observed=38 matched=38 new=0  retired=0 registry_before=38
digest ffbf6e1efbbd21532f2b87530dc6ffbc0c5decd336a758c1872a3ebedbcdd4ef
sha    63d62fae14f76530802daed5034daf07d940960f
```

The fixture is a binary; never regenerate it with a later extractor, because its purpose is to be a
map that Stage A actually wrote (S02 upgrade tests, SchemaConstraintTests trigger fire on an upgraded map).

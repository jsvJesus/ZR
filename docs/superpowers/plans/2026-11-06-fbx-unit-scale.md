# FBX unit scale correction

**Request:** Imported models should have their intended physical dimensions at
instance scale `1 1 1`, without manually setting `100 100 100` each time.

**Cause:** `AssimpContext.Scale = 0.01f` shrinks every FBX, including the supplied
metre-authored file (`UnitScaleFactor = 100`). The existing barracks asset is
0.086239 m high instead of approximately 8.624 m. The previous import pipeline
also omitted `PostProcessSteps.GlobalScale`, which applies Assimp's file-unit
conversion (`UnitScaleFactor * 0.01`) to metres.

**Implementation:** Remove the fixed root scale and enable `GlobalScale`. Mark
newly imported `.zrmodel` files as version 2; keep both versions readable with
the same binary layout. When the original output is version 1, import a new
`.meters.zrmodel` alongside it. Keep original model data and existing instance
transforms stable, including the user's compensating scale of 100. Skip existing
version-2 outputs and report a conflicting legacy corrected-output path.

**Verification constraints:** No tests, test applications, builds, dependency
restore or project execution. Inspect sources, binary headers and dependency
API/documentation, then check the diff. Do not modify existing assets or levels.

- [x] Trace the unconditional scale and inspect existing version-1 model dimensions.
- [x] Verify Assimp's FBX file-scale / GlobalScale path in its public source.
- [x] Correct conversion and add version-aware, non-overwriting repeated import.
- [x] Document the corrected-copy workflow for existing imports.
- [x] Review reader compatibility, repeated imports and whitespace.

**Static review:** Confirmed `GlobalScale` in the exact installed AssimpNetter
6.0.5 XML API documentation. Both full-model and bounds readers use the shared
version-1/version-2 header validation. New writes use version 2. Reviewed fresh
imports, legacy outputs, repeated corrected imports, unsupported versions and
occupied legacy `.meters` paths. Existing model and level files were only read.
`git diff --check` passed. Actual conversion and rendering were not run.

# SpeedTree categories implementation plan

**Goal:** Read the reorganized SpeedTree library, browse by folder, and collide with bark in Tree and Bush (Grass has no collision).

**Architecture:** Keep CTREE105 loading and derive categories from the first directory below speedtree. Resolve moved model/texture references against the current library. Build shared branch-only triangle meshes and combine their instances with scene collision when entering play mode.

**Constraints:** Work in the user's workspace. Do not create/run tests or test applications. Do not build. Verify source and asset data statically. Preserve the existing level.json edit and resource files.

- [x] Add folder category metadata and relocated resource resolution; inspect all existing CTREE headers and texture references.
- [x] Add category selection to the editor browser and batch selection; derive scatter kind from the model directory.
- [x] Convert branch triangle strips to collision meshes for Tree/Bush and integrate into gameplay ground, movement, occupancy, and camera queries.
- [x] Review source changes, call sites, selection transitions, transformed collision bounds, and whitespace with git diff --check.

**Review focus:** Duplicate file names must not pick an arbitrary resource; old level paths must still load when uniquely resolvable; filtering must clear stale placement; generated instances need the same collision rules; terrain attachment, yaw and scale must match rendering.

**Asset inspection:** 284 CTREE105 files (Tree 245, Bush 19, Grass 20), no layout/index issues in the read-only metadata audit. All 2109 nonempty DDS references resolve; none are missing or ambiguous. 33 Tree and 16 Bush models contain no Branches geometry; their model information explicitly reports this limitation.

**Verification:** Independent read-only review found no material issues. `git diff --check` passed. No builds, tests, or application launches were performed; compilation and runtime behavior remain unverified. Concurrent edits to `src/Editor/MainWindow.axaml` and `bin/packs/levels/Escape/level.json` were left intact.

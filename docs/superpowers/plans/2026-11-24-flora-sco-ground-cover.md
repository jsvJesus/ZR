# Flora SCO/SCB ground cover implementation plan

> Execute inline with superpowers:executing-plans. User authorized proceeding in this session.

**Goal:** Connect dense GroundCover to the SCO/SCB assets in `bin/packs/objects/flora`.

**Architecture:** Keep the existing document, deterministic placement, brush and instanced renderer. Reuse SourceModelReader for geometry and bounds; update its permitted paths, ground-cover catalog/settings and source-material texture resolution.

**Tech stack:** C#, existing World and Avalonia/DX11 Editor projects.

**Spec:** User-approved short design in this conversation: grass/flowers/ferns/small stones from flora; one entry per SCO/SCB pair; omit separate LOD variants; load local materials/textures; preserve brush, automatic coverage and saves.

## Global constraints

- No tests, test applications, project builds or application launches; source/data inspection and static checks only (AGENTS.md).
- Preserve the user's modified level/environment JSON files.
- No asset copying, conversion, automatic regeneration, commits or publishing.
- Keep legacy `.model` ground-cover saves readable; do not guess replacements for unrelated old model names.

## Review focus

- SCO/SCB pairs and SCB-only models: one catalog entry, correct reader and bounds.
- Nested flora directories, case differences and LOD suffixes: consistent discovery and no duplicate LOD plants.
- Material texture names without a path: resolve DDS beside the source model; preserve existing texture-path priority.
- Missing assets or malformed geometry: visible diagnostics, no crash or silent save reset.
- Existing ground-cover documents: unchanged format, masks, seed, undo/redo and placement constraints.

## Tasks

- [x] Update GroundCoverCatalog and GroundCoverSettings to accept `.sco`/`.scb`, retain `.model` compatibility, classify ground plants and exclude trees/bushes and separate LODs. Prefer SCO in pairs.
- [x] Permit flora SCO/SCB paths in SourceModelReader; use its bounds API directly in GroundCoverGenerator rather than the BigWorld road fallback. Keep binary-only files readable.
- [x] Add model-local texture lookup in SourceMaterialReader and source foliage sidedness where needed. Surface ground-cover rendering warnings through its document/UI; explain an empty catalog.
- [x] Document usage and limitations; inspect actual material/texture references and full diff, run `git diff --check`, verify no unrelated edits.

## Execution notes

- Initial inspection: 166 SCO + 166 SCB files, 34 XML profiles and 44 DDS files at flora root; Materials is its only subdirectory. Catalog currently finds only `.model`, and SourceModelReader rejects `objects/flora/*.sco` paths.
- Ruling: work in the user-specified checkout, on existing `feature/terrain-import`; no extra worktree or commits. The user explicitly approved proceeding with the concrete design.
- Ruling: AGENTS.md overrides TDD/build steps in workflow skills. Validate only through source and asset inspection.
- Ruling: retain the old RoadCatalog metadata fallback for `.model` bounds only, preserving existing BigWorld saves. SCO/SCB bounds go directly through SourceModelReader.
- Static asset inspection found 121 eligible ground-cover models after excluding LODs, trees and bushes. All 166 SCB companions have the supported `FADC0038` signature and version 0. This verifies file headers, not full binary decoding or rendering.
- Independent static review found no actionable issues in the scoped diff. Reviewed path confinement, SCO/SCB dispatch, legacy `.model` compatibility, unchanged generation/seed/mask/undo and warning delivery while the camera is stationary. `git diff --check` passed; compilation and runtime behavior remain unverified because project builds, tests and application launches are prohibited.

### Existing incomplete asset references

18 material names occur in the 121 eligible SCO files. The following references are absent from the flora folder; they are reported during model loading, and no substitute is guessed:

| Missing resource | Affected models |
| --- | --- |
| `Materials/INB_grass_01_d.mat` | `am_*` and most `inb_*` grass/flowers |
| `Materials/09_-_Default.mat` | `Grass_Sparse_02.sco` |
| `Materials/01_-_Default.mat` | `inb_des_grass_patch_01` through `04` |
| `Materials/_new_crossroads_grass.mat` | `WZ_Grass_002.sco` |
| `Materials/21_-_Default.mat` | Three `WZ_Rock_*` base models |
| `WZ_Grass_Tall_01_D.dds` | `WZ_Grass_Tall_01.sco` |

Restoring these exact source resources is required for their authored appearance. Unrelated existing texture variants cannot establish the missing material definitions.

# Terrain flatten implementation plan

> Use superpowers:executing-plans. Project restrictions override test/build steps.

**Goal:** Flatten terrain and keep attached scene models at their relative terrain height.
**Architecture:** Terrain height notifications bracket committed sample changes. SceneData samples affected anchors before edits and projects models afterward, without adding object history entries. Object history retains anchors and reprojects snapshots when restored.
**Tech stack:** Existing C# World and Avalonia Editor, no new dependencies.
**Spec:** `docs/superpowers/specs/2026-11-15-terrain-flatten-design.md`

## Constraints and review focus

- No tests, test apps, builds or editor launches; static checks only.
- Preserve existing UI changes and user changes in bin/packs/levels/Escape.
- Handle stroke boundaries, zero-effect edits, strength limits, holes, mesh-step interpolation, and terrain undo.
- Keep scene bounds, selection, rendering, collision, dirty flags and history consistent.
- Old objects.json versions remain readable; attachment flags and anchors round-trip in the new version.
- Save terrain and object transforms together through both editor save entry points.

## Tasks

- [x] Extend TerrainBrushMode and implement fixed-height flattening plus before/after height notifications, including strong smoothing and undo.
- [x] Add lazy scene anchors, region-based updates, per-object attachment, and history projection.
- [x] Persist attachment data; connect loaded levels and coordinated saving.
- [x] Add editor controls, stroke height capture and explanation.
- [x] Review sources, XML and diff; independent source review; record limits.

## Verification record

- Static C# syntax parsing (tree-sitter) of all 22 changed/new C# files completed with zero parse errors. No compilation, assembly execution, project launch or tests were used.
- `git diff --check` passed; existing Avalonia XML was parsed successfully.
- Source review covered flat-height capture/reset, enum indices, normal/strong smoothing, terrain undo, sparse height previews, object history projection, bounds and collision updates, and lazy catalogue refresh.
- Independent source review completed. Addressed combined save-button availability and persistence failures: anchors are written before terrain, projected against actual disk heights on load, and retained even when undo returns objects to their source transforms.
- Existing objects documents v1/v2 remain readable; v3 stores attachment settings and stable anchors. A complete successful save includes both objects and terrain; terrain storage still uses its existing per-file atomic writes, with anchor-based recovery for partial terrain writes.
- Runtime visuals, C# type checking and UI interaction remain unverified under the no-build/no-run restriction.

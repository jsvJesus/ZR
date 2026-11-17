# Vegetation placement parity

User requests the same terrain placement and manipulation workflow as scene objects.
Source investigation: vegetation currently places on plain LMB behind a checkbox,
has no gizmo or keyboard handling, and ignores follow-terrain/height settings on placement.

- [x] Use Ctrl+LMB to place, LMB to select, and the existing object gizmo geometry/renderer for move and rotation.
- [x] Add cancellable plant dragging with Shift snapping; route release/capture/focus/mode changes safely.
- [x] Preserve full yaw/pitch/roll through rendering, collision, editing, and persistence; old saves default to zero pitch/roll.
- [x] Replace the placement checkbox with tool buttons and concise instructions, apply terrain attachment immediately without jumps, and synchronize properties after drag.
- [x] Static review of input lifecycle, transforms, persistence, and diffs; no builds, tests or app launches.

Keep existing category/loading/collision changes and unrelated user level edits.

Completed 2026-11-17. Source review covered mouse/key routing, capture/focus loss,
mode changes, commit/cancel, terrain offsets, serialization and shared render/collision
transforms. Invalid placement parameters now block placement instead of falling back
to a selected model with defaults. `git diff --check` passed. No builds, tests or
applications were run; compilation and interactive behavior remain unverified.
Individual manipulation applies to manually placed instances; generated vegetation
remains controlled by scatter rules.

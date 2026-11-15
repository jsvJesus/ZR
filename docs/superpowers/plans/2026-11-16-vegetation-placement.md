# Vegetation placement parity

User requests the same terrain placement and manipulation workflow as scene objects.
Source investigation: vegetation currently places on plain LMB behind a checkbox,
has no gizmo or keyboard handling, and ignores follow-terrain/height settings on placement.

- [ ] Use Ctrl+LMB to place, LMB to select, and the existing object gizmo geometry/renderer for move and rotation.
- [ ] Add cancellable plant dragging with Shift snapping; route release/capture/focus/mode changes safely.
- [ ] Preserve full yaw/pitch/roll through rendering, collision, editing, and persistence; old saves default to zero pitch/roll.
- [ ] Replace the placement checkbox with tool buttons and concise instructions, apply terrain attachment immediately without jumps, and synchronize properties after drag.
- [ ] Static review of input lifecycle, transforms, persistence, and diffs; no builds, tests or app launches.

Keep existing category/loading/collision changes and unrelated user level edits.

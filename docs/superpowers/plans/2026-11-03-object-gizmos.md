# Object scene gizmos implementation plan

User correction: numeric transform fields alone do not provide the requested scene
editing experience. Add visible movement arrows and rotation rings to selected objects.
Continue instance-only prefab behavior from the Objects Editor design.

Use world X/Y/Z axes (red/green/blue), authored object origin as pivot, approximately
96 screen pixels independent of camera distance. Draw handles over scene depth and
pick the same projected geometry. Modes: Move (1) and Rotate (2), also panel buttons.
Keep the numeric fields and existing selection bounds. Gizmo hits take precedence over
object selection. Dragging captures the mouse; release commits; Escape, right click,
capture/focus loss, leaving object mode, level replacement and close cancel the preview.
Keep the camera still while dragging. Shift snaps to 0.5 m / 15 degrees.

One gesture must produce one undo record. Preview transforms preserve the complete
authored affine matrix, update only the selected instance and spatial cells, and expose
fresh collision on demand. Committing/cancelling finalizes collision bounds and refreshes
the object list once; intermediate preview notifications update only selected fields.
Block save/Play/mutations during a gesture without disabling its own input mode.

All verification is static per AGENTS.md: no tests, test apps, builds, editor launches
or shader compilation. No new dependencies or source asset changes.

## Tasks

- [x] SceneData preview transaction and incremental spatial update; history/cancel/save guards.
- [x] Shared ObjectGizmo geometry, constant-screen-size projection and handle hit testing;
  Move/Rotate enums and frame; DX11 arrows/rings renderer reusing grid shaders.
- [x] Native viewport gesture math/capture/cancel, mode keys, camera pause and preview events.
- [x] Panel mode buttons and lifecycle locks that preserve the active captured gesture.
- [x] Independent static review, whitespace/source consistency checks, documentation.

## Verification record

Independent static review covered affine drag math, preview/cancel/history, incremental cells and collision refresh, panel and global locks, mouse capture/focus/close behavior, shader layout and vertex capacities. The far-plane picking mismatch was corrected and re-reviewed: shared projection, line and cone clipping use the actual scene near/far interval. No-op gestures do not create history entries. XAML and whitespace were checked statically. No tests, builds, applications or shader compilation/launches ran; rendering and native gesture behavior remain unverified at runtime.

# Objects Editor

User request: select, edit and delete scene objects, including individual objects
inside a prefab. The user explicitly chose changes to the selected map instance only.

The editor will select a leaf model with the mouse or a filtered object list and show
its bounds. Properties cover position, yaw/pitch/roll in degrees, and XYZ scale.
Delete removes the selected model; undo/redo restore transformations and deletions.
Prefab information identifies the parent, but a leaf action never edits its siblings
or another instance of the same prefab. Scene geometry and collision update together.

Save per-level overrides to objects.json next to level.json, using the existing atomic
temporary-file replacement pattern. Load them whenever the converted level opens.
Save through File/Save, panel save, level switching, and window close. Source LevelData.xml,
shared prefab definitions and mesh files are unchanged. Global prefab edits were the
alternative explicitly declined by the user. Flattening all prefabs into LevelData.xml
would lose source structure and is unnecessary for the chosen instance-only behavior.

Each leaf retains its source object-index path, source signature, original local
bounds, prefab identity and full world matrix. Validate saved signatures before applying
overrides; malformed or incompatible files disable overwriting and retain source data.
Maintain a mutable scene index and rebuild collision bounds after edits. Reuse mesh,
material and physics caches. Picking uses visible model triangles, including objects
without physics, rather than the physics proxy or the entire prefab bounds.

Use existing Avalonia panel/menu patterns and native DX11 viewport input. Editing is
disabled during Play, startup, loading and saving. No new dependencies. No project builds,
tests, test applications or editor launches; source reading and static asset analysis only.

Implementation choices: numeric transform fields, supplemented by world-axis move arrows
and rotation rings per 2026-11-03-object-gizmos.md; leaf-only selection; filtered list capped
for the approximately 29,000 objects in Aspen. Unsupported
world transforms that cannot be represented as scale/rotation/translation remain deletable
and selectable, without silently discarding shear when editing transform fields. Gizmo
translation/rotation preserves the complete affine basis and can edit these matrices.

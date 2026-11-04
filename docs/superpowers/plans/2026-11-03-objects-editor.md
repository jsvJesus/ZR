# Objects Editor Implementation Plan

> **For agentic workers:** Use executing-plans for inline work and scoped implementation/review assistance. Steps use checkbox syntax.

**Goal:** Edit and delete individual scene models, including leaf objects of one prefab instance.

**Architecture:** Retain source identities during expansion, apply per-level overrides,
and update scene spatial/collision indexes in place. Connect an Avalonia object panel
to exact model picking, bounds highlighting, native input and the existing save lifecycle.

**Tech Stack:** C#/.NET 10, Avalonia, SharpDX DX11, existing XML/JSON asset readers.

**Spec:** docs/superpowers/specs/2026-11-03-objects-editor-design.md

## Global Constraints

- No tests, test applications, project builds or application launches. Static verification only.
- Changes affect only the selected map instance; original imports and shared prefab files remain intact.
- Preserve earlier texture-streaming work and other existing workspace changes.
- Work in the user's existing feature/terrain-import checkout; no commits or branch changes required.

## Review Focus

- Repeated prefab definitions: editing one child must leave all siblings/other instances unchanged.
- Deleted or moved objects must update collision and spatial lookup, and undo must restore both.
- Save/reopen must retain edits, with full matrices and validated identities; failed saves retain dirty state.
- Mouse selection must use visual triangles and include decorative objects without collision.
- Play/load/save/close paths must disable mutations and preserve pending edits.

### Task 1: Scene identity, editing and persistence

Files: World/Scene/SceneModels.cs, SourceSceneReader.cs, SceneData.cs;
new SceneObjectTransform.cs and SceneEditStorage.cs; Editor/Levels/LevelWorkspace.cs.

- [x] Give SceneInstance init properties Id, SourceStamp, PrefabName, LocalBounds and VisualLocalBounds.
- [x] Keep source index paths unique through nested prefabs and signatures stable on unchanged source entries.
- [x] Implement SceneData.Find(id), UpdateTransform(id, world), Remove(id), Undo(), Redo(),
  CanUndo, CanRedo, CanSave, IsDirty, Changed; rebuild indexes/collision after mutations.
- [x] Implement SceneObjectTransform(Position, Rotation, Scale): ToWorld() and
  TryFromWorld(Matrix4x4, out SceneObjectTransform); rotation vector is yaw/pitch/roll in degrees.
- [x] Implement SceneEditStorage.LoadAsync(directory, scene, token) and SaveAsync(directory, scene),
  FileName = objects.json. Validate the complete document before applying any overrides.
- [x] Load overrides after reading source objects, before exposing the loaded level.
- [x] Read identity/transform/save paths and statically inspect prefab source data.

### Task 2: Object panel, selection and application integration

Files: new Editor/Objects/ObjectsPanel.cs (split commands if useful),
Viewport/DxViewportHost.Objects.cs, visual picking/highlight helpers;
MainWindow, EditorModes, level save/open/lock and play controls; SceneRenderer/Dx11Renderer;
World/Scene/SceneTriangleTree.cs and a visual picker if needed.

- [x] Add the panel to Objects Editor scene/properties modes with search, capped list,
  selected model/prefab info, numeric transforms, apply/delete/focus/save and undo/redo.
- [x] Extend visual model BVH picking without altering collision geometry semantics.
- [x] Add selected-object bounds highlighting and native mouse/Delete/Ctrl-Z/Ctrl-Y handling.
- [x] Disable editing and clear highlight when leaving object modes or entering Play/loading/saving.
- [x] Integrate pending object saves with File/Save, level switching and close.
- [x] Read all new control/event/lifecycle paths and inspect XAML/static source consistency.

### Task 3: Static review

- [x] Independent static review of source identity, repeated-prefab edits, picking and save lifecycle.
- [x] Address important findings and run git diff --check (including new files).
- [x] Report implemented controls and explicitly state compilation/runtime remain unverified.

## Static verification record

- Asset XML audit: 29,406 expanded leaf models, 29,406 unique source index paths; 569 prefab children from 76 prefab placements. No parent prefab scale fields in Aspen.
- Scoped data review and final integration review completed. Missing required JSON-member validation was corrected with JsonRequired attributes and re-reviewed.
- MainWindow XAML parses as XML with no duplicate named controls and one ObjectsPanel registration.
- Read source identity, matrix round-trip storage, undo/redo, spatial/collision rebuild, visual picking and native input, and save/load/close/play guards.
- Whitespace checks include tracked changes and all new source/document files. No tests, builds, application or shader compilation/launches ran.
- Runtime limitations: triangle picking does not sample cutout texture alpha; the first pick of a cached visual model constructs its BVH. Numeric editing preserves unsupported sheared transforms by disabling those fields.

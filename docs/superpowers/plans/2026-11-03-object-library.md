# Object library implementation plan

User request: browse/display, edit, place, delete and save models from packs/objects.
Continue existing per-instance editing, gizmos, undo/redo and level saving.

Asset inspection: 60,286 files; 18,191 .model, 18,278 .visual/.primitives pairs,
3,053 DDS. Model/visual metadata uses packed BigWorld sections (magic 0x62A14E45),
primitives use binary sections (0x42A14E65), not the existing SCO format.

## Scope and approach

- Read the actual BigWorld formats with confinement and validation, route objects/*.model
  through the scene model/cache pipeline. Preserve models/*.sco and MPX/PHX behavior.
- Catalogue .model files recursively, search by relative path, cap displayed results.
- Show selected catalogue model in a separate preview viewport; model editing means
  instance transforms (the user's existing Objects Editor), not vertex/topology editing.
- Select catalogue entry, enable placement, click a world surface to add an instance.
  New instances use existing selection/gizmos/transform fields/delete/history/save.
- Persist additions with stable generated IDs in objects.json alongside source overrides.
  Read existing version-1 files without losing current Aspen edits. Validate complete
  documents and loaded models before applying; failures retain files/read-only behavior.
- Use authored collision when supported/present, never substitute visual triangles as
  an unannounced physics fallback. Report missing dependencies/unsupported formats.
- Additions/undo/redo and gizmo previews update spatial lookup and collisions coherently.
- Avoid main-thread disk/model decoding during browsing/placement; bound background work
  and guard stale async results on selection/level/mode changes.

## Work

- [ ] BigWorld packed sections, primitive geometry, material/texture and authored physics reading.
- [ ] Scene addition identity/history/index and version-compatible persistence.
- [ ] Catalogue, preview, placement interaction and existing editor lifecycle integration.
- [ ] Static asset/source audit, independent review and documentation.

Constraints: AGENTS.md forbids tests, test apps, project builds and launches. All
verification uses source reading and static resource analysis. Do not touch live
packs/levels/Aspen/objects.json (user changes), SDK assets or unrelated work.

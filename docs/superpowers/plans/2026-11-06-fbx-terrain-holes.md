# FBX terrain with preserved holes

**Accepted scope:** Import only the ground mesh, rasterize its heights and retain
openings for buildings. The user places all scene objects manually. First source:
`bin/packs/import/levels/Escape/Escape_Terrain.fbx`.

**Architecture:** Reuse Assimp's metre-based FBX conversion. Rasterize the highest
surface at height-grid vertices and cell centres. A cell without complete surface
coverage is a hole. Store a separate bit mask, shared by gameplay height queries,
editor picking and both terrain shaders (including distant overview). Use a finer
mesh sampling step for these imported levels. Preserve legacy levels without masks.
Create the level atomically in its own directory; do not overwrite existing levels.
Use an initial neutral ground material rather than importing scene objects.

**Editable terrain:** Add basic raise/lower/smooth and cut/fill controls. Keep
height and hole changes in a shared terrain document, rebuild terrain resources
after edits, save changed tiles/mask/overview with level saving, and retain changes
through level switching and closing. Keep editing unavailable during Play/import.

**Coordinates:** Shift the mesh's minimum X/Z to the new level's zero origin;
record that offset. Keep Y and metre scale. Default grid spacing is 1 m, configurable
at import; preserve holes at the chosen resolution. Reject excessive sample counts.
For multiple intersections, use the highest surface and report the approximation;
a heightfield cannot retain multiple storeys at the same horizontal position.

**Files/components:** Terrain holes storage and level metadata; FBX rasterizer and
level writer; shared terrain edit document; renderer mask and sampling integration;
import settings/commands and terrain edit panel; save/close and gameplay integration.

**Constraints:** No tests, test applications, builds, shader compilation or editor
launches. Read source and existing FBX only; review XML, shader interfaces, binary
layouts, cancellation, bounds and whitespace. Preserve user changes and source files.

- [x] Add holes and shared editable terrain data with backward-compatible storage.
- [x] Import FBX ground into terrain heights, mask, overview and manifest.
- [x] Integrate mask and sampling with rendering, picking and gameplay.
- [x] Add Escape-compatible import UI, editing controls and saving lifecycle.
- [x] Review statically and document usage and current limits.

## Review record

Independent read-only review found three important issues; all addressed: unsupported
overlapping heights are reported (see the subsequent correction below), moving takeoff over a hole keeps finite feet
and enters falling, and overview updates touch only edited tiles rather than loading
an entire legacy level. Clean CPU tiles are bounded to 64; dirty tiles stay resident
until saved. Hole-only revisions update the GPU mask without discarding terrain meshes.

Deferred minor: height brush operations still rebuild the resident terrain resources,
which temporarily falls back to overview geometry while fine tiles stream back in.
Documented this behavior. Imported materials, automatic objects, exact vertical walls
and overhang preservation are outside the accepted ground-heightfield scope.

Escape source static inspection: FBX 7400, 65,662 control points, 359,949 polygon
vertex entries, unit scale factor 1 and model scale 100. Assimp file-unit conversion
and node pre-transformation handle the declared scale. No import code was executed.

Verification: XML documents parsed successfully; changed/new C# files parsed with
Roslyn syntax APIs (no compilation or emission), with zero syntax errors. Reviewed
CPU/GPU cell mask coordinates, shader t5 bindings, legacy defaults, save lifecycle,
finite character positions at holes, bounds and staging ownership. Whitespace checks
passed. Tests, builds, shader compilation, application launches and actual conversion
were not run. User changes in Aspen and imported barracks assets were preserved.

## Follow-up: valid ground blocked by six overlaps

The user's updated ground-only Escape FBX triggered the previous unconditional
overlap rejection at six samples. Multiple vertical intersections do not establish
the presence of buildings or a second terrain layer: a single ground mesh can fold
or have steep seams. Removed the fatal check. Rasterization retains its existing
upper-envelope selection and unchanged coverage/hole-mask logic. Count each affected
grid vertex once; show a warning after a successful import and persist it in
`import.json`. Real multistorey geometry remains an explicitly documented heightfield
limitation. Verify by source/syntax inspection only, without builds, tests or imports.

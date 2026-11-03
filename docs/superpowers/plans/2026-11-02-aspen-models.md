# Aspen scene models and character collisions

**Goal:** Opening the existing Aspen level loads its source models, materials and prefab instances, in the editor and play mode, with character collisions.

**Source:** `packs/import/levels/Aspen/LevelData.xml`, `packs/models`, `packs/Prefabs/*.xml`.

**Constraints:** No tests, test applications or builds. Verify by source/data inspection and `git diff --check`. No changes to source assets or existing imported terrain. No commits or branch changes.

**Design:** Read source scene objects when opening the imported level, as roads already do. Expand prefabs recursively with child * parent transforms and guard cycles. Resolve both `packs/models/` and `data/objectsdepot/` to the existing models directory. XML rotation x/y/z means yaw/pitch/roll in degrees. SCO vertex positions are relative to their CentralPoint. Cache shared model geometry; stream detailed meshes on demand, draw repeated objects using DX11 instancing. Obtain bounds from matching SCB position headers when possible, otherwise SCO vertices. Load collision geometry synchronously when needed, with camera/spawn prefetch, so physics never depends on whether an object was rendered.

**Physics:** Preserve terrain-only height APIs for editing and roads. Use authored MPX/CPX proxies and PHX metadata independently of visible SCO/SCB models. Gameplay queries retain actual capsule support at stairs and ramps. Use a spatial scene index and shared physical acceleration structures for walls, floors and ceilings. Respect `PhysEnable=false` and `CollisionPlayerOnly`. Camera rays also hit physical geometry. Crouching/standing must respect head clearance. Existing animation selection stays intact. See `2026-11-02-aspen-physics.md` for the authored-physics correction.

**Shared contracts:** `ZoneReborn.World.Scene` provides SceneBounds, SceneVertex (48 bytes), SceneMaterial, SceneMesh, SceneModel, SceneInstance, SceneData and SceneModelCache. Renderer and collision implementation work in separate files against these contracts. No physics or GPU resource is duplicated per map instance.

- [x] Implement scene/prefab parsing, model bounds, SCO geometry and material/texture resolution.
- [x] Implement shared triangle collision queries and capsule motion.
- [x] Implement instanced model rendering with view/distance culling, bounded GPU uploads and resources.
- [x] Connect loading, renderer lifecycle, spawn/locomotion/camera and status reporting.
- [x] Inspect all referenced source files/materials/prefabs and review all changes statically.

**Review focus:** CentralPoint handling; yaw/pitch/roll and prefab composition; UV seams and per-face materials; thin walls/doorways/multiple floors/ceilings; cancellation and scene replacement; bounded memory/uploads for 28k instances; missing assets reported rather than preventing the whole level from opening.

**Static asset audit:** 29,406 expanded instances, 7 with collision disabled; 656 unique models; 28 used prefab XMLs; 1,985 model/material pairs; 1,341 unique DDS textures (689 diffuse, 652 normal). No missing referenced models, materials or textures. All 654 text models use the supported ten-component vertex and triangle/UV format. Two binary files named `.sco` use SCB v0; their layout was independently matched against paired text/binary models before implementing it. All referenced DDS formats are supported by the existing reader. Largest referenced geometry has 513,437 vertices and 257,488 triangles.

**Review corrections:** Preserve swept support on stair lips and descending ledges, including low-speed motion; no direct collision bypass during jump anticipation; apply capsule-radius ramp offset to gameplay ground height; retain full contact normal during falls on steep faces; draw scene transparency after trees and characters; share one vertex buffer across material groups; permit one over-budget GPU upload rather than dropping large valid assets. Source and whitespace checks completed. No builds, tests, shader compilation or application runs were performed.

**Practical limits:** Detailed visual meshes load lazily; authored physical proxies are read independently while opening the source scene. Visual distance is 1,800 m, with per-frame upload budgets and normally 128 MiB each for geometry and textures; one oversized asset may exceed those budgets. Available DDS mips reduce residency to 1,024 pixels. Collision uses static authored proxies, without rigid-body simulation. Animated source props are shown in their authored mesh pose. Rendering/physics behavior still needs an application check after the user builds the editor.

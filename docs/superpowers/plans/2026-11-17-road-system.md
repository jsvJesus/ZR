# Road replacement plan

Spec: docs/superpowers/specs/2026-11-17-road-system-design.md.

- [ ] New model/storage; disconnect DAT/material loading.
- [ ] Smooth curve sampling and bounded road/rail/path meshes.
- [ ] Dynamic opaque rendering and procedural surfaces.
- [ ] Editor creation, point dragging, profile controls, saving.
- [ ] Shared collision and vegetation-clearance integration.
- [ ] Remove obsolete references; documentation and static review.

Parallel ownership: geometry worker owns RoadSpline.cs/RoadMesh.cs; renderer worker owns Viewport/Roads and Road.hlsl; root owns model/storage/editor/integration. All checks are static; no builds, tests or app launches.

# Visual collision ground support correction

**Problem:** Stepping onto the thin doormat at the imported barracks entrance
switches the character to an airborne animation. Capsule sweeps collide with
both sides of every triangle, but ground queries and slide classification
reject downward-wound faces, including open visual sheets.

**Static evidence:** The existing `Mil_Barracks_01.zrmodel` contains 7,341 triangles.
The `mtl_floor_fence_01_S1` material consists of two downward-facing triangles
at local Y 0.0005987883. Nearby floor triangles face upward at Y 0.0005148053.
The doormat blocks the capsule above the lower floor, but its downward normal
previously excluded it from `TryGround` and `TrySnapDown`. Snapping to the lower
floor then fails `CanOccupy` because the capsule intersects the doormat.

**Change:** Mark triangles of `ScenePhysicsMesh.IsVisualFallback` as double-sided,
carry that property through instance transforms, and use the upward orientation
of those faces for ground/ramp classification. Preserve authored physical mesh
winding. Keep the positive swept contact-normal requirement, slope limit,
step-height limit and full-capsule clearance checks.

**Verification:** Read the existing binary model and source control flow; review
upward support, underside ceiling contact, steep faces and reflected transforms.
Run whitespace checks. Do not create/run tests, build or launch applications.

- [x] Identify the doormat geometry and trace collision versus support classification.
- [x] Preserve two-sided fallback surfaces through the shared triangle tree.
- [x] Use matching ground normals in surface, snap-down and slide queries.
- [x] Review the final diff and static invariants.

**Review result:** All three ground/ramp classifications use the same normal
orientation. The fallback flag survives world transforms, including reflections.
Authored collision meshes retain their previous winding checks. Snap-down still
rejects downward contact normals, and capsule clearance and step-height checks
are unchanged. `git diff --check` reported no whitespace errors. Runtime behavior
was not exercised under the project's static-only verification constraint.

**Separate finding:** The existing imported model is about 0.086 m high, while
the source FBX's dimensions are measured in metres. The importer's unconditional
0.01 root scale needs separate correction. Existing model and level assets were
not modified while addressing the doormat's support classification.

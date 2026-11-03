# Aspen authored physics and stair support correction

**Problem:** Initial scene collision used visible SCO triangles. Authored MPX/CPX proxies were ignored. The controller also discarded real capsule support when its center ray saw a floor more than 0.2 m below, triggering fall/landing animation on steps. Center-ray plane extrapolation at ramp crests could create hovering support.

**Changes:**

- [x] Read physical proxies independently of render geometry/materials and cache each model once.
- [x] Parse PHX metadata, prefer available MPX for static queries, otherwise CPX; never manufacture physics from SCO when no proxy exists.
- [x] Decode MPX versions 7/9 with 8/16/32-bit indices and CPX version 13 with hull versions 6/7, bounded/finite geometry and index validation.
- [x] Keep cooked local coordinates unchanged: their CentralPoint offset was already applied during cooking.
- [x] Include authored physical bounds in scene broad phase; leave visual model loading independent.
- [x] Query actual capsule support before clearing Grounded, preserve support at step edges/ramps, and use the same support for descending landings.
- [x] Check the actual sphere/plane contact against finite ground triangles instead of testing only a center ray.
- [x] Final static integration review and whitespace verification.

**Static asset inspection:** 656 referenced models. 622 MPX files: 621 version 9, one version 7; 157 use byte indices, 461 use 16-bit indices, four use 32-bit indices. Their position/index sections contain 2,563,523 vertices and 3,840,726 triangles, with no invalid values or indices found. 34 CPX files: 15 hull version 6, 19 version 7; 1,388 vertices, 2,640 triangulated faces, no invalid polygon references. 32 CPX files also have MPX; the two CPX-only assets are bandana and dune_buggy_dmg. 32 decorative models have neither authored proxy and are excluded from collision, with diagnostics. PHX found for 598 models; type numbers are source-engine-specific, so static geometry selection follows available authored files. Dynamic PHX flags are reported; rigid-body simulation remains outside the static scene implementation.

**Verification constraints:** No tests, test applications, builds, shader compilation or project runs. Inspect source and existing binary/XML data only. Keep all existing model-loading work and original assets intact.

**Final review:** Corrected a source-local name shadowing the physics cache; inspected the authored-geometry-to-BVH pipeline, bounds/transforms, finite ramp contact, support-first grounded movement, descending landings and ceiling rejection. XML `CollisionPlayerOnly` is inherited through prefabs: character queries keep these walls, camera rays exclude them. No automatic `_playeronly` filename substitution was introduced without an explicit source reference. Tracked and new source files passed whitespace inspection. Runtime stair behavior remains unverified under the project restrictions.

**Format references:** NVIDIA PhysX [mesh loading](https://raw.githubusercontent.com/NVIDIAGameWorks/PhysX-3.4/master/PhysX_3.4/Source/GeomUtils/src/GuMeshFactory.cpp), [serialization flags](https://raw.githubusercontent.com/NVIDIAGameWorks/PhysX-3.4/master/PhysX_3.4/Source/GeomUtils/src/mesh/GuMeshData.h), [convex hull loading](https://raw.githubusercontent.com/NVIDIAGameWorks/PhysX-3.4/master/PhysX_3.4/Source/GeomUtils/src/convex/GuConvexMesh.cpp). The old mesh geometry prefix was also checked against the actual MPX files; the newer PhysX runtime itself rejects mesh versions <= 9.

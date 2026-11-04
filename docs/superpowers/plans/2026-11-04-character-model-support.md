# Character support on model collision

> Implementation follows systematic-debugging and executing-plans in the current workspace.

**Goal:** Stop unsolicited vertical movement and keep ordinary steps traversable without jumping.

**Evidence:** The user observes both bouncing and blocked steps. Source inspection found that
slide constraints are classified using the already projected displacement; a first contact can
add Y and disable the wall constraint at subsequent contacts. Step clearance currently requires
the entire 0.3 m lift, even when a smaller lift is sufficient. Support sweeps start exactly at
the previous contact and cannot recover a small penetration rejected by CanOccupy. Step limits
check capsule feet rather than the actual tread touched by the rounded capsule.

**Resource inspection:** Authored MPX stair proxies contain horizontal treads with approximately
0.193 m (stadium) and 0.25 m (outpost) rises. Citadel stair_2 uses an approximately 22-degree ramp.

**Constraints:** No tests, test applications, builds, or project launches. Preserve user level
data. Keep the 0.3 m step limit, 40-degree face limit, authored collision and jump controls.

- [x] In SceneCollision.cs, retain the original horizontal-movement classification through
  all slide passes and classify steep collisions by both authored face and contact normal.
- [x] Recover support from a clear 0.01 m lift, retaining the original maximum drop. Limit
  accepted tread contact height separately from capsule feet, including rounded edge contacts.
- [x] In TerrainSurface.cs and CharacterLocomotion.Collision.cs, propagate the grounded tread
  limit and use available upward clearance for steps. Recover the previous support only if
  a blocked, unsupported slide adds height beyond the allowed ramp rise plus 0.01 m.
  Horizontal wall slides off real ledges must still fall.
- [x] Review flat ground, ramps, stair edges, low ceilings, tall walls, descending ledges and
  airborne landing call paths statically; inspect whitespace and document runtime limits.

**Review outcome:** Separate static review found that an unconditional blocked-movement
rollback would prevent wall-tangent ledge falls. Recovery was narrowed to excess upward
motion beyond the slope allowance plus 0.01 m; scoped re-review reported no findings.
Source/caller inspection and `git diff --check` completed. No tests, builds, simulations,
or applications ran. Actual traversal, contact smoothness and 120 Hz query cost remain
unverified at runtime under the project restrictions.

## Follow-up: entry stairs still block movement

The user supplied an entry-stair screenshot after running the previous change. Exact model
identity was requested; the saved editor camera is elsewhere and does not identify this house.
The user explicitly excludes `_playeronly` resources. No replacement or additional proxy from
those files is enabled; existing ordinary MPX selection remains unchanged.

- [x] Keep nearest points relative to capsule feet in sweep, penetration recovery, occupancy,
  and snap surface-height calculations. Previously the geometry routines reconstructed
  world-space float points before subtracting them for a normal; at Aspen X/Z coordinates
  a float ULP exceeds the 0.0001 m contact tolerance. Existing world-triangle quantization
  remains; this change removes the additional error in closest-point reconstruction.
- [x] Return the actual accepted tread-contact height through snap/support queries. Retain
  it only when accepting a normal move, step, recovery, or landing. Use the greater of feet
  and last accepted surface height as the baseline for the next 0.3 m step. A rejected probe
  must never promote a high surface into this baseline. This corrects the overly strict
  feet-based surface cap on successive rounded-cap stair contacts.
- [x] Statically review saved overloads/callers, accepted/rejected support branches, jumps,
  ceilings, tall walls and ledges; verify whitespace. No runtime claim for the supplied
  screenshot without identifying its model and observing actual traversal.

Separate static review of both follow-up corrections reported no findings. `git diff --check`
passed. Read-only traversal of Aspen and its 28 used prefab definitions found zero model
references containing `_playeronly`; no automatic substitution is present. No tests, builds,
simulations or project launches were performed.

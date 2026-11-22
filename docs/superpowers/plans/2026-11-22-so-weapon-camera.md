# SO weapon stance and camera implementation plan

> Execute inline using superpowers:executing-plans, with source inspection and static resource analysis replacing builds and tests as required by AGENTS.md.

**Goal:** Restore SO weapon profiles, continuous hand poses, and a coherent TPS → FPS ADS → TPS → bored sequence.

**Architecture:** Keep the existing locomotion, track-12 equipment sequence, avatar IK, clothing corrections, and camera collision probes. Read missing weapon limits and camera profiles from the original SDK instead of inventing per-weapon transforms. Use one view ray for the TPS marker and weapon tracker.

**Tech stack:** C#, System.Numerics, existing BigWorld resource readers and Direct3D renderer.

**Spec:** User attachment `Pasted text.txt`, stages 1–8; reference screenshot. The repository prohibits builds, tests and test applications; verification here is static only.

## Review focus

- Combat activity during jumps/posture/equipment transitions must update the timer without interrupting authored clips.
- Reversing stance during a hand-grip blend must retain the evaluated finger/forearm pose.
- Right-hand and Track weapons must use their own profiles; released hands must not constrain weapon reach.
- Turns must retain bored/aim/combat upper-body layers.
- Camera collision, aspect ratio and ADS must agree with the view ray and marker.

## Tasks

- [x] Audit all 78 weapon entries, StalkerBase2 actions and referenced animation resources, hand_control, AvatarTrackers, view_control, player_speed_settings and PlayerAvatar bytecode.
- [x] Extend CharacterWeaponDefinition profiles with gun limits and left-hand release. Apply limits in animated HP_GUNR_LIMIT space before GUNRW child adjustments and IK; retain equip carry and clothing correction.
- [x] Blend grip layers from the currently evaluated pose. Keep movement matching and source phase/blend times, remove forced crouch clip replacement, preserve the correct upper-body stance on turn overlays.
- [x] Load tracker_global camera profiles; select status/movement from matched actions. Preserve skeleton FPS eyes and collision; replace the displaced cross with a small white dot on the view ray.
- [x] Read the complete diff, statically parse changed C# and source resources, review call sites/input/state transitions, run git diff --check, and record evidence and runtime limitations in docs/so-stance-camera-audit.md.

## Verification record

Static resource audit: 78 weapons, 1950 weapon profiles, 25 camera profiles, 140 supported actions, 108 referenced animation binaries; no missing references or invalid selected frame ranges. Independent review identified two convergence defects; terrain ray intersection and HP_AIMSYS offsets were corrected. Final syntax/format checks are recorded in the audit report. Builds, tests and Play were not run; compilation and visual behavior remain unverified.

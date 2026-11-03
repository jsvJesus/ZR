# Character weapon handling implementation plan

> Execute with superpowers:executing-plans. Project rules override test/build steps: source and asset inspection only.

**Goal:** Draw and holster the equipped weapon in Character Editor and Play using original weapon metadata, animation clips, hand targets and grips.

**Architecture:** Keep outfit selection separate from runtime weapon state. Load SDK weapon types/tracker profiles in the catalog, add an independent track-12 Equip/Dequip player over locomotion, switch attachments at the authored in/out boundary, and solve hands against the weapon's exported anchors. Original locomotion capabilities select armed body animations; finger actions remain independent tracks. Preserve existing 45-degree turns and jump variants.

**Controls:** Character Editor draw/holster buttons; H toggles in Play. No shooting/reload feature is implied. Existing outfit persistence continues to store the selected weapon, not transient animation frames.

**Sources:** SDK WEAPON.pyson, hand_control.pyc_dis, AvatarTrackers.pyc_dis, clweapon.pyc_dis, game_items/__init__.pyc_dis, StalkerBase2.model. Holster slot chooses animation family; GunType and per-item tracker choose locomotion/hand pose. Full-body clips are shared where the original game shares them.

- [x] Catalog: retain category/GunType/slot/speed, tracker offsets and grip names; resolve original holster.
- [x] Animation library: armed locomotion, posture, 45-degree turn, equip/dequip and grip clips; retain reversed clips and authored held tails.
- [x] Runtime: two-phase state machine, independent masked overlay, attachment transfer and hand IK including legacy exported targets; handle repeated input and outfit replacement.
- [x] Editor: type details/filter, draw/holster controls and state feedback; Play keyboard toggle.
- [x] Review: source/asset references, syntax trees and whitespace. No tests, test apps or builds.

Review cases: no weapon; pistol/SMG/rifle/launcher; original legacy M1911/M79/M202 anchors; negative speed anomaly; reverse/tail clip ranges; rapid toggles; crouch/sprint/jump; clothes rebuild while holding; removing/replacing weapon; focus/level changes during async loads.

Static evidence: 20 changed/new C# files parsed without syntax errors; AXAML parsed as XML; no whitespace errors. Inspected 108 selected matching/weapon actions, 79 animation binaries and 5,056 channels; no missing references, unsupported channel types, malformed key ranges or trailing data. The three authored frame tails are retained deliberately. Source review caught and corrected disabled support-hand IK for legacy M202. Visual animation/IK continuity remains unverified because project execution is excluded.

Rulings: use original shared holster animation families plus per-item tracker/grip data; preserve numeric GunType even where a gun's appearance suggests a different name. An invalid nonpositive equip speed falls back to normal playback. Weapon changes await the old holster sequence; clothing changes retain a held weapon. Outfit controls wait for pending Equip/Dequip to finish. Existing working texture changes remain in the workspace.

## Follow-up: detached wrists and movement offsets

User reported detached hands with body_adrenalin_woh and ManHands, including sprint/jump. Static inspection of the saved outfit confirmed the jacket exports Arm/ArmRoll/ForeArm but no Hand/ForeArmRoll, while ManHands exports the full chains. The per-rig IK solver therefore moved the bare hands and skipped the sleeves. IK now runs only on the complete avatar, then each garment receives the matching bone's before/after correction; garment-specific inverse binds remain intact. A BlendBone receives its parent's correction even when the garment has no wrist branch.

Original hand_control.__check_for_weapon transfers Track to Right while airborne. Runtime now blends to this carry through jump/landing and restores tracking afterwards. AvatarTrackers.modelMatchNotifier selects the tracker key from the matched locomotion action; runtime now does the same after movement selection, retaining the previous ground key through jump/posture clips. The right-hand locomotion capability uses the original bored/sprint weapon profile independently of this temporary airborne attachment.

Validation remains source/asset inspection only: confirmed forearm and BlendBone mappings on the actual jacket and hands, parsed the three changed C# files, and inspected call order/signatures and whitespace. No tests, test applications, project builds or visual execution.

## Follow-up: left wrist in jumps and sprint grip reach

The airborne carry change released left-hand IK but left its grip layer fully active. In the current AK105 metadata, handgrip_left_cevie_incombat assigns priority 6 to LeftForeArmRoll, so this layer also overrides the free wrist/hand chain. Both the left grip layer and left IK now use the same fading attachment weight, settling to exactly zero during right-hand carry.

Read-only first-pose inspection of AK105's sprint resources and tracker transforms found left grip distances of 0.862 m (slow) and 0.824 m (fast) from the shoulder, versus 0.563 m total arm length. The previous solver clamped the arm and kept the gun target out of reach. The tracked rigid weapon is now projected within both arm reach spheres before attachment blending and hand IK, preserving bone lengths and the authored gun rotation. Unreachable blended targets retain their animated wrist rotation beyond the SDK's 0.03 m rotation threshold. Wrist twist is shared with the exported ForeArmRoll node using the SDK's left/right weights of 0.5/1.

Validation: inspected active item 5284, its grip masks and idle/run/sprint tracker data, source hand tracker setup and all changed call signatures. Both changed C# files parse without syntax errors; whitespace inspection passes. No builds, tests, test applications or visual execution. Appearance remains to be checked in the user's Preview.

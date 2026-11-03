# Character Editor Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox syntax. User approved implementation of the linked design on 2026-11-01; proceed without another approval round.

**Goal:** Change clothing, gear and weapons through the right panel on the existing level Preview.
**Architecture:** Original item catalog produces an immutable outfit and composed model selections. The character loader supports the selections and weapon attachments; editor UI manages inspection, replacement and local persistence.
**Tech Stack:** C#, .NET 10, Avalonia, existing SharpDX renderer and packed BigWorld resources.
**Spec:** `docs/superpowers/specs/2026-11-01-character-editor-design.md`.

## Global Constraints

- Do not create/run tests or test applications. Do not run builds. Verify sources and asset data statically.
- Preserve existing working changes and Locomotion. Work in the explicitly supplied workspace.
- Reuse the open level and Preview. Clothing is equipment selection, not mesh editing.
- Keep last successful character on errors/cancellation; ignore stale asynchronous loads.

## Review Focus

- Multi-part suits, overridden nude slots and tints must compose correctly.
- Rigid weapon bind pose and attachment origins must not inherit avatar animation channels.
- Leaving inspection or changing level during loading must not resurrect old characters.
- Right panel must remain usable with free cursor; normal Play mouse behavior must resume.
- Persist only successfully applied outfits, retain camera/position, handle missing SDK/assets visibly.

## Interfaces (namespace ZoneReborn.World.Characters)

- `CharacterPartSelection(string Model, IReadOnlyDictionary<string,string> Tints, string? AttachmentNode = null, string? OriginNode = null)`.
- `CharacterOutfit(int[] ItemTypes, int? WeaponType = null)`.
- `CharacterEquipmentItem`: TypeId, Name, Category, Slots (int[]), Models (string[]), Tints, IsWeapon; display via Name.
- `CharacterEquipmentCatalog.Read(AssetPaths, CancellationToken)`; `Items` (IReadOnlyList), `DefaultOutfit`, `Equip(CharacterOutfit, CharacterEquipmentItem)`, `Remove(CharacterOutfit,int)`, `Compose(CharacterOutfit)` (CharacterPartSelection[]). Additional original metadata stays internal.
- `CharacterLoader.Load(AssetPaths, IReadOnlyList<CharacterPartSelection>, CancellationToken)` and `Rebuild(AssetPaths, IReadOnlyList<CharacterPartSelection>, CharacterAsset, CancellationToken)`; existing overloads remain valid.
- Viewport exposes catalog/outfit load, enter/exit inspection, apply outfit; UI uses the catalog to filter and compose selections.

### Task 1: Original catalog and outfit composition

Files: new `src/World/Characters/Equipment/*` only.
- [x] Implement safe tolerant PYSON literal reader (no execution), source catalog and above types.
- [x] Preserve SlotType/OverrideSlots/AlternativeModelsList/Tint; add baseline nude body + teeth, original Adidas defaults, multi-part clothes; slot conflicts replace items.
- [x] Compose weapon on HP_weaponback using matching origin. Validate source model paths; no invisible template entries in ordinary catalog. M1911 uses its exported HP_Pistol origin/anchor.
- [x] Read sources and catalog data to check defaults, suit combinations and original slot rules. No tests/builds.

### Task 2: Geometry, materials and attachments

Files: `CharacterLoader.cs`, optional loader helper files, `CharacterAsset.cs`, `Formats/PrimitiveReader.cs`.
- [x] Support composed model selections, reuse existing animation/library on Rebuild.
- [x] Support rigid xyznuvtb alongside existing skinned formats, proper node palettes, no-render materials and selected model tints; bounded uniform colour multiplier is passed to the existing shader.
- [x] Separate weapon bind rig from animated avatar rig, apply inverse weapon origin times avatar attachment matrix. Exclude attached equipment from floor offset; retain existing garment/shoe ground reference.
- [x] Statically inspect all 367 catalog base/alternative/weapon references, 325 unique visuals, references and matrices. No tests/builds.

### Task 3: Viewport and right panel

Files: new `src/Editor/Characters/*`, `Viewport/DxViewportHost.Character.cs`; integrate MainWindow, modes, Play and native mouse handlers.
- [x] Add Character Editor menu and category/search/item/equipped lists, equip/remove/reset controls and loading/error status.
- [x] Inspect actor on existing level, cursor free, RMB orbit/wheel zoom; no movement input during inspection. Restore previous Play state on exit.
- [x] Cancel obsolete operations, rebuild transactionally at existing position/yaw/camera, keep selected outfit for later Play and local JSON persistence. Small settings commit is synchronous after GPU installation to avoid Stop/Close races.
- [x] Handle no level, Stop, mode switch, level replacement and closing; preserve existing Editor changes.
- [x] Statically check UI event wiring, asynchronous state transitions, XAML XML and `git diff --check`.

### Task 4: Integration review

- [x] Independent read-only review of catalog/loader/UI integration and the five review-focus cases.
- [x] Fix actionable findings and re-read changed sources; run only static verification.
- [x] Record implementation and verification limits in docs; report changed behavior and no runtime verification.

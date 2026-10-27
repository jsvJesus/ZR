# Editor Play Character Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans inline. The user requested the actual button after the design discussion; continue implementation without another approval gate.

**Goal:** Play spawns an original textured, animated Stalker Online character beneath the editor camera and switches to a third-person orbit camera; Stop restores editing.

**Architecture:** World owns direct BigWorld decoding, skeleton sampling and Terrain queries. Editor owns transient Play lifecycle, orbit input and DX11 rendering. Originals stay read-only.

**Tech Stack:** .NET 10, System.Numerics, Avalonia, SharpDX DX11, existing DDS reader.

**Spec:** docs/superpowers/specs/2026-10-27-editor-play-character-design.md

## Global Constraints

- No asset conversion or derived character files.
- Work in the user's current feature checkout: their running editor must receive this change.
- First stage is spawn, original idle animation and third-person view; movement comes later.
- Do not replace a failed character load with a dummy object.

## Review Focus

- Malformed counts/offsets must fail before allocations or out-of-range reads.
- Skin matrices must preserve bind pose and match per-renderSet bone palettes.
- Terrain boundaries and rendered sampling must match the spawn height.
- Stop, close and failed/late asynchronous loads must release resources and restore the camera.
- Terrain and character draw passes must set all required DX11 state.

### Task 1: Resource decoding

Files: World/Characters/Formats/{PackedSection,PrimitiveReader,AnimationReader}.cs and tests/CharacterChecks.
Interfaces: PackedSection.Read(path), PrimitiveReader.Read(path), AnimationReader.Read(path), managed format records.
- [x] Add a dependency-free console verification project with real asset assertions and malformed fixtures; run RED.
- [x] Decode binary sections, vertex/index streams and animation channels; test known counts, valid indices, weights and complete stream consumption.
- [x] Run checks GREEN and document format sources.

### Task 2: Composite character and pose

Files: World/Characters/{CharacterAsset,CharacterLoader,CharacterPose}.cs.
Interfaces: CharacterLoader.Load(AssetPaths), CharacterPose.Sample(asset,time), skinned mesh output.
- [x] Add checks for complete body, texture dependencies, finite poses and bind-pose identity; run RED.
- [x] Load compatible source clothing/body parts and original idle; resolve palettes and inverse bind transforms.
- [x] Run checks GREEN against real assets at multiple animation times.

### Task 3: Terrain and orbit

Files: World/Terrain/TerrainSurface.cs, World/Characters/ThirdPersonCamera.cs.
Interfaces: TerrainSurface.TryGetHeight(x,z,out y), ThirdPersonCamera.Update/Rotate/Zoom.
- [x] Test known triangle heights, tile boundaries, invalid coordinates and orbit direction/zoom; run RED.
- [x] Implement bounded tile cache, matching rendered grid interpolation and Terrain camera obstruction.
- [x] Run checks GREEN.

### Task 4: Editor Play and rendering

Files: Editor/Play/*, Editor/Viewport/Characters/*, MainWindow.axaml, DxViewportHost, Dx11Renderer and level command guards; packs/shaders/Characters/Character.hlsl.
Interfaces: viewport StartPlayAsync/StopPlay, Play state event; character renderer Draw with active camera matrices.
- [x] Add renderer and transient lifecycle, orbit input, Play/Stop/Esc and panel hints.
- [x] Preserve editor camera, handle focus/resize/close/cancel/errors, disable conflicting commands.
- [x] Build to isolated output while existing editor is running; run graphics smoke checks where available.

### Task 5: Verify and review

- [x] Run all console checks and Editor build; inspect actual Play screenshot and correct rendering issues.
- [x] Request one fresh whole-change review; address important findings and rerun affected checks.
- [x] Update user instructions, record validation and any remaining limitations.

## Execution ledger

- Initial design was documented but not implemented; user explicitly expects the actual editor feature now.
- Ruling: execute inline in Z:/ZoneReborn; a second checkout would not update the user's running project and another approval question would repeat the original failure.

- Verification: CharacterChecks passed on original assets; Editor build: 0 warnings, 0 errors. Graphical verification passed actual rendering, Stop button, camera restore, second Play, cancellation, and out-of-bounds rejection.
- Review: independent whole-change review identified an omitted camera endpoint collision check; a real Terrain fixture reproduced it RED, then GREEN after checking the endpoint and retaining the last clear sample.
- Ruling: exported visual rest transforms need not equal the original bind pose in every bone; use a hand-derived parent/inverse-bind fixture and actual animated human bounds instead of assuming every rest palette is identity.
- Output: bin/Editor.exe updated in the current checkout; native DX11 frame captured to bin/character-research/viewport-play.png.

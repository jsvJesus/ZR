# Terrain Import Implementation Plan

> Execute task by task using the approved design. User instructions override skill defaults: no builds, tests, or application execution. Independent binary analysis and DDS work may use parallel agents.

**Goal:** Import Arizona, Aspen and California into our level format and open them in the Editor with Terrain materials.

**Architecture:** A graphics-independent World library owns asset paths, source readers, models and storage. Editor owns import/open commands and DX11 Terrain resources. Original assets stay read-only.

**Tech Stack:** C# / .NET 10, Avalonia, existing SharpDX DX11, HLSL; no new external packages.

**Spec:** `docs/superpowers/specs/2026-10-26-terrain-import-design.md`

## Global Constraints
- Only Terrain in new names, documentation and UI; discover legacy source filenames dynamically.
- Do not build, run tests or launch the application.
- Work in the current checkout on a feature branch so ignored local source assets remain accessible.
- One responsibility per file, organised by feature.
- Import destination must not destroy an existing level.

## Review Focus
- Source chunk boundaries, height scale and layer numbering must match original binary data.
- Cancellation and failed conversion must not publish incomplete output.
- Untrusted stored asset paths must remain inside the selected asset root.
- Large California data must not allocate a complete expanded mesh.
- DDS pitch, channel masks, normal format, buffer lifetime and resize must remain correct.

## Task 1: Decode source binary data
- [x] Inspect source headers, payload boundaries and per-map settings.
- [x] Determine height, normal, layer mask and index layout with evidence.
- [x] Record exact layout in focused import readers and format documentation.

## Task 2: Shared World library
- [x] Create `src/World/World.csproj`, solution entry and Editor/Client references.
- [x] Add `Assets/AssetPaths.cs` and `Assets/AssetPathResolver.cs`.
- [x] Add separate level manifest, Terrain descriptor, chunk and layer models under `Levels/Models` and `Terrain/Models`.
- [x] Add bounded versioned binary storage under `Terrain/Storage` and JSON handling under `Levels/Storage`.
- [x] Add DDS reader and mip models under `Textures/Dds`.

## Task 3: Source conversion
- [x] Read source INI and texture references under `Terrain/Import/Configuration` and `Terrain/Materials`.
- [x] Read source chunks and coverage under `Terrain/Import/Binary`.
- [x] Add `Levels/Import/LevelImporter.cs`: cancellable conversion, progress, staging, source report, atomic publication.
- [x] Keep textures shared and preserve original layer ordering and source payloads as needed.

## Task 4: Editor rendering
- [x] Add focused geometry, material, texture and shader resources under `Viewport/Terrain`.
- [x] Add depth buffering and Terrain integration to `Dx11Renderer`.
- [x] Render only selected chunk detail and bound GPU memory; dispose resources on replacement/resize.

## Task 5: Editor workflow
- [x] Add import and open-level commands; show conversion progress and errors.
- [x] Move level UI responsibilities into `Levels` and existing camera/mode handlers into focused files.
- [x] Open `level.json` from the native levels directory and save its camera without losing Terrain metadata.
- [x] Document usage and perform static diff/API review only.

## Progress
- Design approved by user; naming and file-structure requirements added.
- Feature branch created; no build/test execution will be used.

- Shared World and Editor operations reviewed independently without compilation.
- Renderer reviewed independently; far-map UV tiling finding fixed with separate diffuse/normal transforms and clamp sampling.
- Overview geometry added for whole-map visibility; detailed tile edges use transition skirts.
- XML source parsed and git diff --check reviewed; no build, test, shader compilation or application launch.
- No generated levels have been published by this session; conversion runs through the new Editor import command.

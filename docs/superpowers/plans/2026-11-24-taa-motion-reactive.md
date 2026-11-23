# TAA jitter, geometric motion and reactive masks implementation plan

> Execute using superpowers:executing-plans and independent renderer tasks through superpowers:dispatching-parallel-agents. User explicitly requested all fixes and completion after the static audit/video.

**Goal:** Stabilize jittered TAA and complete skeletal, rigid-object, billboard vegetation motion and material-aware history rejection.

**Spec:** The user's Stage 4 requirements and explicit instruction to revise all TAA/jitter, preserve previous deformed vertices/bones for character/clothes/arms/weapons, previous object World, previous billboard orientation and future wind positions, and reactive masks for transparency/grass/weapons.

**Architecture:** Keep the current DX11 forward renderer and HDR ping-pong history; add separate R16 encoded raw-depth history, synchronized with color ping-pong. Upgrade motion to RGBA16F: xy = current minus previous jittered UV; z = log2(1 + previous clip.w); w = valid previous geometry (1 or 0). Add R8 reactive target. Reconstruct base static motion from depth, then overwrite visible opaque/cutout pixels with geometric motion; translucent geometry writes only a conservative reactive mask using max blending and read-only depth. TAA removes jitter delta from motion, accumulates on a fixed output grid, validates history against the supplied previous surface depth, and uses authored reactivity plus color/depth rejection. Contact shadows are composed before temporal resolve.

**Tech stack:** C#, System.Numerics, SharpDX Direct3D11, existing HLSL shaders.

## Constraints

- AGENTS.md: no tests, test apps, builds, shader compilation or project launches. Source/static inspection only.
- Preserve existing flora implementation, user level/environment settings and groundcover.json; do not commit, regenerate levels or change user tuning.
- No wind animation exists today. Implement exact current/previous billboard reconstruction and describe the extension point for future deformation; do not add invented wind motion.
- No new dependencies in the project. Separate renderer tasks own disjoint files.

## Shared interfaces

- SceneRenderer.RenderVelocity(RenderTargetView motionTarget, RenderTargetView reactiveTarget, DepthStencilView depthTarget, Matrix4x4 currentViewProjection, Matrix4x4 previousViewProjection, Vector3 cameraPosition, bool historyValid, int width, int height).
- TreeRenderer.RenderVelocity uses the same signature. Record the camera basis/world/visible geometry from the color pass, not shadow passes; history is valid only for consecutively rendered compatible geometry.
- CharacterRenderer.RenderVelocity keeps its existing parameters, inserts RenderTargetView reactiveTarget immediately after motionTarget, and appends float firstPersonBlend. Preserve the visible mesh-set filter. Color and velocity must use identical transition visibility.
- All geometric velocity shaders output float4 motion : SV_Target0 and float reactive : SV_Target1 for opaque/cutout. PSReactive outputs float mask : SV_Target0 for translucent/reactive-only draws; no motion/depth writes on blended surfaces.
- Invalid/new geometry uses current positions as previous input, w=0, reactive=1. Never evaluate uninitialized previous matrices/vertex buffers.
- Shared HLSL include `PostProcess/TemporalMotion.hlsli`: ClipToTemporalUv(float4), EncodeTemporalDepth(float), MakeTemporalMotion(float4 currentClip, float4 previousClip, float valid), returning float4 motion. Add helper include before renderer shader usage.
- Parent owns allocation, integration calls for objects/roads/groundcover/trees/character, reset/cut handling, base surface motion, TAA resolve and contact-shadow ordering.

## Work

- [x] Parent: temporal motion include and buffer allocation/callback integration; deterministic centered jitter and complete cuts/mesh-domain resets.
- [x] Object task: previous World by stable instance ID/model, consecutive visibility/history safety, instanced velocity pass and material reactive-only translucent pass. Preserve opaque grouping/transparent sorting and shadow behavior.
- [x] Vegetation task: previous world and camera billboard basis for actual visible color draws, exact leaf cutout, motion/reactivity; no shadow contamination or invented wind.
- [x] Skeletal task: previous deformation snapshots and expected depth, transition parity, reactive/cutout/transparent handling, invalid history and resource lifecycle.
- [x] Parent: fixed output-grid resolve, correct previous depth/disocclusion, sky/silhouette treatment, geometric-motion-only reduction, reactive sampling, move contact shadows before resolve; remove obsolete global FPS feedback reduction.
- [x] Static review: input layouts, cbuffer packing, MRT formats/order, depth domains, frame-history lifetime, transparent ordering and resource cleanup. Independent full review; `git diff --check`. Document runtime verification limits.

## Review focus

1. Stationary camera and jitter on/off/strength changes: no artificial motion from Halton offsets.
2. FPS/TPS threshold/depth changes, teleport, roll/FOV cuts, resize and rendering mode changes invalidate all dependent history.
3. Alpha cutouts exactly match color; translucent layers mark reactivity without replacing opaque motion or depth.
4. New/disappearing/replaced models and nonconsecutive visibility cannot reuse stale transforms/vertices.
5. Motion z is the previous *surface* depth, including deformed and billboard positions, not the current point projected backward.

## Progress

- Source audit and video established stationary jitter-related instability and partial dynamic coverage. User authorized implementation. Existing worktree is the user-requested checkout on feature/terrain-import; work in place without commits.
- Ruling: AGENTS overrides workflow TDD/build/launch steps; validate sources and dataflow only.
- Independent geometry review found and resolved reactive-mask overwrites (all opaque MRT1 writes now MAX blend), FPS near-plane mismatch (captured color projection provides SV_ClipDistance0) and late world transparency over FPS geometry (world transparency/weather finish before FPS hands).
- Independent core review found and resolved different depth/color coordinate grids and false sky support. Color is reconstructed with a bounded cubic filter; surface selection requires positive actual filter contribution. Exact raw center depth is retained separately, with its own previous raster UV. RGB uses fixed-grid reprojection. Sky rays remove both jitter phases correctly.
- Local weapon/FPS and vegetation masks respond to actual geometry motion under the same current camera, excluding camera/jitter. Static cutout edges retain accumulation instead of receiving full rejection.
- Source ABI sizes: TAA 240 bytes, base frame 272 bytes, scene motion 256 bytes/144-byte instance stride, tree motion 416 bytes/56-byte vertex stride, skeletal motion 384 bytes/40-byte current and 12-byte previous vertex strides.
- Color material alpha for character cutouts now consistently includes DiffuseColor.a in both color and velocity shaders.
- Final source checks: 11 changed/new C# files parsed with tree-sitter without syntax error nodes; all 8 direct includes in 7 changed/new shader files resolve; five paired C#/HLSL constant buffers match field order and byte sizes. Independent core and geometry re-reviews reported no remaining actionable issues after fixes. `git diff --check` passed. These are static checks, not C# type checking or shader compilation.

## Verification limits

No project, test, shader compiler or renderer was executed. Static C# parsing, source/API inspection, C#/HLSL buffer-layout checks, shader include resolution and independent reviews are used instead. Runtime stability, GPU performance and visual elimination of the recorded wobble still require checking in the editor. No wind animation is added: future wind deformation must be evaluated for current and previous states in both matching tree reconstruction paths.

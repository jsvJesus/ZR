# Stage 3 AO completion

User specification: depth/normal GTAO or SSAO, edge-preserving bilateral denoising,
temporal depth rejection, editor enable/intensity/radius/quality and JSON persistence,
coherent Sky Visibility, natural geometry contacts without excessive darkening.

Constraints: AGENTS.md forbids creating/running tests or applications and running
builds. Verification is source inspection, static analysis and independent review.
Keep existing Terrain materials/UVs and editor grid behavior. Do not change user
environment settings. No commits or publishing are requested.

Architecture: retain the existing forward renderer and half-resolution AO passes.
Capture the actual, fog-attenuated ambient contribution in a second HDR target.
Apply AO only to that contribution; combine broad sky visibility with local AO
using the more occluded visibility for sky light and local AO for residual bounce.
Make the FPS depth buffer sampleable and use its matching projection throughout
postprocessing when active. Preserve world depth for subsequent world draws.
Keep core/temporal implementation isolated from renderer integration.

## Tasks

- [x] Renderer: allocate, clear, bind and dispose ambient MRT; have lit shaders
  write ambient with the same layer/transparency operations as scene color.
- [x] Composition: extract shared sky visibility helpers; combine ambient-only AO
  with sky visibility, preserve direct/specular/fog; bloom sees AO-composited HDR.
- [x] FPS: expose sampleable combined world/FPS depth and use its actual full
  view-projection for normals, AO, history and contact shadows.
- [x] AO: finish robust core, bilateral/upscale edge handling and normal-aware
  temporal history with depth rejection and current-neighborhood clamping.
- [x] Audit settings/save wiring, source consistency, resource hazards, all
  output/constant layouts and lifecycle; independent review and final diff check.

Review focus: AO disabled matches original color; steep flat terrain remains
unoccluded; indoor sky occlusion does not double-darken sky light; local furniture
contacts remain visible indoors; FPS depth agrees with projection; odd resolutions
and resize/toggle transitions are safe; moving/appearing objects reject stale AO.

Visual acceptance remains unverified until the user runs the editor; this is not
permission to run it or to claim visual acceptance based on static inspection.

## Implementation record

Retained horizon-based SSAO, explicitly permitted by the specification; the UI
now identifies it as SSAO. Ambient HDR capture follows original material colors,
layer weights, alpha blending, puddle reflection and fog attenuation. Sky and local
visibility share sky-light occlusion, while local AO also affects residual indoor
bounce. Screen contact shadows retain their original presentation/bloom ordering.
History uses AO, half-float linear depth and octahedral normals at the same memory
cost as before; reprojection predicts the previous texel's receiver-plane depth.
Depth/normal validation, current-neighborhood variance bounds and AO-change
rejection handle stale history conservatively without object motion vectors.

Verification: independent source reviews of AO/history and renderer integration,
including a scoped contact/bloom ordering re-review, found no actionable defects.
Static include/brace scan and git diff --check passed. No tests, builds, shader
compilation, applications, commits or publication were performed.

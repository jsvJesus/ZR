# Aspen texture streaming repair

User report: many model surfaces are white and textures flicker in the editor.
Verification is restricted to source reading and static asset analysis by AGENTS.md.
No builds, tests, test applications, or editor launches are permitted.

## Evidence

- SceneRenderer limited residency to 256 textures and 128 MiB. Material resolution
  for all 656 models (including two binary SCOs) finds 1,341 distinct diffuse/normal
  DDS files, with no missing material or texture references.
- Those mip chains occupy about 843.85 MiB with a 1,024-pixel cap, 227.35 MiB
  at 512 pixels, and 58.39 MiB at 256 pixels, before completing missing mip chains.
- UploadReadyTextures ran before Draw marked current textures as used. A needed
  texture could be evicted before its draw and requested again as a white fallback.
- Texture requests came from Draw and were limited to two pending files. Repeated
  misses in earlier draws could prevent later materials from ever entering the queue.
- Two referenced 1,024-square DDS files have no mip chain:
  g3_props_military/Textures/mil_wall_wirefencebar_detail_01_diffuse.dds (BC3) and
  g3_blds_military/Textures/mil_bld_aircraft_hangar_concrete_01_diffuse.dds (BC1).
  This is an additional source of distant texture aliasing, separate from cache churn.

## Changes

- [x] Collect and pin the complete visible material working set before allocating.
- [x] Remove the 256-entry texture limit; keep the 128 MiB byte budget.
- [x] Reserve a base mip chain for every demanded texture before assigning more detail
  to nearby objects. Replace estimates with actual mip sizes as files arrive.
- [x] Retain the resident CPU mip tails so detail can be reduced without losing a
  texture or rereading disk. Replace resources only after their new upload succeeds.
- [x] Queue missing diffuse and normal maps before upgrades, independently of draw order.
- [x] Complete single-level DDS mip chains in the bounded background reader; retain
  supplied mip chains unchanged. Decode BC1/BC2/BC3 only when mip generation is needed.
- [x] Keep selected BC resource top levels block aligned.

Format reference: [Microsoft BC1/BC2/BC3 layout and resource alignment](https://learn.microsoft.com/en-us/windows/win32/direct3d10/d3d10-graphics-programming-guide-resources-block-compression).

## Verification

Read the material resolution, streaming, replacement/disposal, shader sampling,
BC decoding, mip filtering, and upload-budget paths. Static source/asset checks only;
runtime appearance and compilation remain unverified under the project restrictions.

Independent static review caught a completion race between demand preparation and
upload: exact mip metadata must also be published when consuming every ready read,
including one that completed after preparation. This was corrected before completion.

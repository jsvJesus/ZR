# Residual stationary TAA jitter stability

User reports residual world shimmer/wobble with stronger jitter after the previous TAA work. Continue fixing the implementation; do not reduce jitter strength or overwrite environment settings.

Constraints: AGENTS.md forbids tests/test apps, builds, shader compilation and editor launches. Validate sources/static layouts only; work in the current checkout without commits.

Confirmed source paths: fixed-grid reconstructed RGB is clipped against raw-phase statistics; static cutout masks shorten temporal accumulation; one previous raw depth sample can falsely reject thin geometry; products of two negative cubic lobes can select the wrong supporting surface. GTAO additionally counts projection jitter as movement and snaps AO history to one texel.

- [ ] Replace signed cubic interpolation with a positive reconstruction filter; derive current color, weighted moments, surface and reactive support from the same jitter-centered footprint.
- [ ] Retain bounded per-pixel temporal confidence in existing color-history alpha (positive geometry, negative sky). Trust mature stationary, depth-compatible history instead of treating sampling disagreement itself as reactive motion. Motion/material/depth incompatibility still reduce or invalidate history.
- [ ] Validate disocclusion against the relevant four discrete previous depth samples, never interpolating foreground/background depth values.
- [ ] Make authored cutout/fade reactivity depend on actual geometry motion; preserve transparent and invalid-history rejection.
- [ ] GTAO: add current/previous jitter to constants, remove jitter delta from motion attenuation, and use per-tap depth/normal-validated bilinear history AO.
- [ ] Independent source review, C# parse, C#/HLSL layout/include checks and git diff --check. State runtime limits honestly.

Interface: GtaoEffect.Render appends Vector2 currentJitter, Vector2 previousJitter after quality. AoConstants/HLSL adds Vector4 Jitter after BlurAxis. Parent updates the sole call in PostProcessManager. GTAO history remains in raw raster coordinates; only travelPixels subtracts jitter delta.

No new GPU textures are required for color confidence: previous color alpha's signed value is now history sample confidence, retaining the sign used by presentation for sky dithering. Separate raw-depth history remains unchanged.

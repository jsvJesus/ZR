# FBX model import implementation plan

**Goal:** Import static FBX assets from `bin/packs/import/models` into
`bin/packs/models2`, including their texture dependencies in `models2/Tex`,
and expose them to the existing object preview, placement and persistence workflow.

**Architecture:** AssimpNetter reads binary/ASCII FBX, triangulates and bakes node
transforms, axes and centimetres into the editor's metre-based scene coordinates.
A versioned `.zrmodel` file stores scene geometry and pack-relative material paths;
runtime loading does not need the original FBX or its external texture directories.
DDS textures are copied; other supported image formats and embedded images are
converted to RGBA DDS. Texture names include a content hash to prevent collisions.

**Constraints:** No tests, test applications, builds or editor launches. Verify by
source reading, static resource inspection, project XML parsing and `git diff --check`.
Preserve source assets, existing imported assets and user level edits. Work in the
user's requested checkout; do not commit or change branches.

## Work

- [x] Add versioned imported-model storage and route `models2/*.zrmodel` through
  scene bounds, model loading and visual collision fallback.
- [x] Add FBX conversion, texture resolution/copy/conversion and per-file reporting.
  Resolve original absolute/relative paths first, then an unambiguous filename in
  the import tree or a user-selected texture folder. Missing or ambiguous dependencies
  fail that model explicitly. Publish model files atomically after textures are ready;
  skip existing model files to preserve cache identities and existing placements.
- [x] Add model import and optional texture-folder selection to the object library.
  Browse both `objects` and `models2`, keep legacy catalogue entries accessible,
  refresh after import, and coordinate import lifetime with editor busy/close handling.
- [x] Review source, package APIs, local FBX texture dependencies, paths and lifecycle;
  document usage and the limits of static verification.

## Review focus

- Absolute texture paths and identical filenames in different source directories.
- Embedded and non-DDS textures; portable material paths after importing.
- Unit/axis conversion, material assignment, UV channels and triangle winding.
- Partial batch failure, cancellation, atomic writes and existing outputs.
- Imported model placement, collision fallback and reload from `objects.json`.

## Decisions

- Use AssimpNetter 6.0.5 and StbImageSharp 2.30.16, whose public source APIs and
  package metadata were inspected. No dependency restore is required for static work.
- Static scene meshes are the scope; animation and skinning are not imported.
- The supplied FBX references 22 distinct DDS files under
  `Z:/XraySDK/editors/gamedata/textures`; all were found by static inspection.

## Verification record

- Self-review covered binary reader/writer field order and 48-byte vertex stride,
  relative texture paths, publication order, catalogue navigation, instance reload,
  collision fallback, cancellation and close/save coordination.
- Inspected the XML API documentation inside the exact AssimpNetter 6.0.5 NuGet
  archive, including the .NET 6 public API used by this net10.0 project. Corrected
  the texture-mapping enum to `TextureMapping.FromUV` during that review.
- Parsed project XML and ran `git diff --check` without errors.
- Read the supplied FBX binary metadata without running import code: Y-up,
  UnitScaleFactor 100, identity object transform, 4,395 source control points,
  22,023 polygon-vertex entries. All 22 referenced DDS files exist: 20 DXT1 and
  two DXT5, maximum dimensions 2048 by 1024.
- Tests, builds, dependency restoration and application launches were not run.
  Actual conversion and rendering remain unverified at runtime under the user's
  explicit static-verification constraint.

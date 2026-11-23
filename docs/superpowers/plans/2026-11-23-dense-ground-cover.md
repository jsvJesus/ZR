# Dense ground cover and Terrain brush

User scope: preserve the existing generator for trees/bushes; separate dense grass
and small stones using objects/flora under this project's packs; support procedural
Terrain material coverage and painting/erasing with a Terrain brush. Roads and
objects must block both painting and candidate generation. Preserve existing level
content and settings. AGENTS forbids tests/apps/builds; static verification only.

Architecture: independent GroundCoverDocument in LoadedLevel, compact sparse
one-metre world-cell paint overrides plus generation settings in groundcover.json.
Deterministic jittered cells create dense cover only near the camera; no tree-style
spacing or 20k global instance limit. Reuse SceneRenderer GPU instancing, lighting,
cutout materials and ambient AO output; bounded draw distance with screen dither.
Rebuild nearby cover on camera chunk/settings/height/road/object changes. Placement
uses the shared Road exclusion and actual model footprints/object bounds.

## Shared interfaces (root integrates these)

Namespace ZoneReborn.World.Vegetation:

- GroundCoverSettings record: string[] Models; float DensityPerSquareMeter=4,
  MinimumScale=0.8, MaximumScale=1.2, MaximumSlopeDegrees=45,
  RoadClearance=1, ObjectClearance=0.5, DrawDistance=40,
  MinimumMaterialWeight=0.5; int Seed=12345, MaterialIndex=-1; bool AutoGenerate.
- GroundCoverDocument: Settings, Revision, IsDirty, CanSave, Warnings;
  SetSettings(settings), Clear(), BeginStroke(), EndStroke(), Undo(), Redo(),
  CanUndo, CanRedo. GroundCoverDocument is independent of trees/scene objects.
- GroundCoverCatalog.Catalog(AssetPaths): IReadOnlyList<GroundCoverCatalogEntry>
  with File/Name/Category, usable ToString. Scan objects/flora recursively .model.
- GroundCoverGenerator.Paint(LoadedLevel, AssetPaths, Vector3 center, float radius,
  bool erase): int changed cells; reuse same placement restrictions as generation.
- GroundCoverGenerator.BuildVisible(LoadedLevel, AssetPaths, Vector3 cameraPosition,
  float radius, int maximumInstances=60000): IReadOnlyList<SceneInstance>.
  Pure deterministic placement apart from geometry caches. Radius/limits bounded.
- GroundCoverStorage.LoadAsync(string directory, CancellationToken token=default):
  Task<GroundCoverDocument>; SaveAsync(directory, document, token=default): Task.
- Root adds LoadedLevel.GroundCover property and load/save/render integration.

Root viewport API for UI (new partial host): GroundCoverBrushEnabled bool,
GroundCoverBrushErase bool, GroundCoverBrushRadius float; event
Action<Vector3,bool> GroundCoverBrushRequested, event Action GroundCoverStrokeStarted,
GroundCoverStrokeEnded. Brush only active in vegetation UI mode and never play.

## Work

- [ ] World: document/settings/catalog/storage; dense deterministic placement and
  brush cell edits, history and road/object/terrain restrictions; tree generator
  ignores non-tree/bush rules while preserving old saved data.
- [ ] UI: VegetationPanel.GroundCover partial controls; separate cover catalog and
  generation/paint/erase, settings/material selection; save and mode hooks;
  original catalog/generator restricted to tree/bush categories.
- [ ] Root: load/document integration, viewport brush routing/preview/capture,
  streamed SceneRenderer ground cover, distance fade and cleanup.
- [ ] Static source checks and independent full review; resolve findings.

No project code execution, tests or builds. No automatic copying from SOnline,
no automatic vegetation regeneration/deletion and no commits/publishing requested.

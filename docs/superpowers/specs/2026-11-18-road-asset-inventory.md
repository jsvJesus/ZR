# Road source asset inventory

Inspected 2026-11-18 from the local `bin/packs` directory. This is an inventory and static metadata review, not a visual or gameplay acceptance report. No project build, test, editor, primitive-mesh loader, or texture decoder was run to produce these counts. The authored level `bin/packs/levels/Escape/roads.json` was not changed.

## Source folders

| Folder | `.model` | `.visual` | `.primitives` | Local `.dds` |
| --- | ---: | ---: | ---: | ---: |
| `objects/roads` | 4,817 | 4,817 | 4,817 | 0 |
| `objects/roads_segments` | 102 | 102 | 102 | 56 |
| Total | 4,919 | 4,919 | 4,919 | 56 |

Every visual in these two folders has a sibling model. The current `SourceModelReader` dispatches BigWorld assets by `.model`, so the catalog deliberately lists models rather than offering bare visuals that the scene loader cannot open. Combined model and visual metadata occupies 5,259,296 bytes. Neither folder's models use parent references or default dye overrides in this snapshot; catalog parsing nevertheless supports both using the existing scene-reader conventions.

`objects/roads` is organized by original location and authored road ID. These are existing location-specific sections, not a general spline construction kit. Names contain road IDs and chunk coordinates, and visual bounds vary substantially; some snow sections span more than 80 metres. Local bounds can be centered around the origin even for baked sections, so the folder/name evidence does not establish a reusable connection frame. Importing one does not reconstruct its original scene placement or road graph.

`objects/roads_segments` is the more useful starting point for reusable modules. It includes `asphalt01`, `asphalt01a`, `asphalt01b`, `asphalt02`–`asphalt04`, `dirt01`–`dirt05`, `sand01`, cross/T-cross/end variants, concrete borders and sidewalks, `railscave01`–`railscave03`, wooden `pathway01`, guardrails, walls, cables, hoses, and snow meshes. These are candidate authored components, not all drivable surfaces. For example, `asphalt01.visual` bounds run approximately X=0..1 and Z=-6.01..6.01, while `pathway01` runs X=0..3 and is a wooden planks/logs material. Orientation, width, pivot, collision and joins require module-specific authoring before automatic placement.

## Location groups

| Location under `objects/roads` | Models |
| --- | ---: |
| anomaly_event_shop | 31 |
| black_forest | 2,094 |
| city_lubech_2 | 11 |
| city_lubech_apartments | 7 |
| dm_new_year_team_snowballs | 11 |
| event_new_year_delivery | 180 |
| event_new_year_main_2024 | 224 |
| inst_elka_defence | 24 |
| inst_event_shop | 80 |
| inst_outlands_paukan | 68 |
| inst_polary_expedition | 14 |
| inst_solo_lib | 1 |
| inst_solo_lubech | 19 |
| inst_solo_shop | 80 |
| inst_vesuvius_dungeon | 7 |
| outlands | 469 |
| outlands_airport | 200 |
| pvp_teamdm_hold_shop | 28 |
| start_station_lesnaya | 124 |
| testers | 2 |
| tunguska | 92 |
| tunguska_caravan | 149 |
| usov_topi | 112 |
| vesuvius_2 | 790 |

## Material evidence and limits

A static PackedSection metadata scan found 81 distinct existing texture references in the canonical `diffuseMap`, `normalMap` and `specularMap` fields, and 64 distinct diffuse/normal/specular/shader combinations. No texture referenced by those three recognized fields was missing after applying the existing `models/` to `objects/` alias. The 64 figure describes source texture/shader tuples, not an executed catalog result; catalog IDs also distinguish alpha and specialized-shader flags.

Examples obtained from actual visual properties:

| Source visual | Diffuse | Normal | Specular |
| --- | --- | --- | --- |
| `roads_segments/asphalt01.visual` | `road_0156b.dds` | `road_0156b_norm.dds` | `road_0145_spec.dds` |
| `roads_segments/dirt01.visual` | `road_dirt0048.dds` | `road_dirt0048_norm.dds` | `road_dirt0048_specular.dds` |
| `roads_segments/pathway01.visual` | `pathway01.dds` | `pathway01_norm.dds` | none |

These example textures live under `objects/roads_segments`. The asphalt specular filename differs from the diffuse stem in its authored metadata; the catalog preserves that reference instead of guessing a matching stem.

The source collection includes standard normal/specular maps, multi-layer masks, UV transforms, reflections, glow, `pixelUp`, and a specialized `road.fx` atlas shader. Unknown/encoded property names are not guessed to be albedo or roughness. Canonical maps are exposed where available; a material without a resolved canonical diffuse map is omitted. `RequiresSpecializedShader`, `AlphaTest` and `AlphaBlend` identify approximation risks. A specular map is not a roughness map, and these assets are not claimed to be converted PBR materials.

## Catalog API and bounds

`RoadAssetCatalog.Load(AssetPaths, CancellationToken)` returns immutable `Entries`, `Materials`, and `Warnings`. `LoadCachedAsync(AssetPaths, CancellationToken)` shares one background scan per `AssetPaths` object; canceling that call cancels the caller's wait, while `Load` supports cancellation of an independent scan. Cached results are a snapshot for that asset-root object.

- `RoadAssetEntry`: `File`, `Name`, `Category`, `Visual`, metadata-only nullable `Bounds`, `MaterialIds`.
- `RoadMaterialOption`: stable `Id`, `Name`, `Category`, `SourceAsset`, `SourceVisual`, verified packs-relative `Diffuse`, `Normal`, `Specular`, source `Shader`, and the three compatibility flags.
- `Search(query, category)` searches model name/path/location; `SearchMaterials(query, category)` also respects every location using a shared material. Categories are `roads/<location>` or `roads_segments`. Full relative paths preserve the finer road-ID grouping.
- Material IDs hash the texture references, shader and compatibility flags. Duplicate texture sets have a deterministic representative source, preferring reusable segment assets. They represent texture sets, not every authored scalar/colour parameter.

Limits: 8,192 models, 4,096 unique texture sets, 50,000 directory entries, 16 directory/model/material inheritance levels, 2 MiB per metadata file, 64 MiB total metadata, and 256 diagnostics. Enumeration and metadata parsing propagate cancellation. Resource references remain within `packs`; absolute/traversal/linked dependency paths are rejected. Authored texture extensions resolve to an existing DDS first, matching `BigWorldModelReader`. No primitive mesh or texture pixels are loaded by the catalog.

## Renderer integration

A nonempty road `MaterialId` starts or shares the background metadata scan. The renderer keeps procedural output until the catalog is ready, then uploads only the selected DDS maps. Missing or unsupported maps produce bounded diagnostics; diffuse failure preserves the procedural surface and normal/specular failures preserve the other maps. Resource use is bounded to 16 material sets and 128 MiB total retained texture payload, with 16 MiB per source DDS and 64 MiB per material.

Texture sampling affects upward asphalt/path pavement faces only. Meter-based UV repetition and procedural road markings are retained. Normals use the UV derivative frame, and the existing weather/lighting model uses the selected specular map and `MaterialWetness`. Rails, ballast, sleepers, shoulders and structures retain their existing material logic. Source layered/alpha effects are not reproduced, and visual texture orientation and normal-map conventions still need editor acceptance once running the editor is authorized.

## P0 terrain invalidation audit

Before the incoming modifier changes, `TerrainRenderer` cleared GPU tiles and overview on `TerrainDocument.HeightRevision`; road resources tracked that revision, hole revision and each road's revision. `TerrainOverviewResources` selected `BuildOverview()` only when base terrain was dirty, otherwise loading the disk overview. A derived-only modifier revision therefore needs an effective-height revision and composed `Tile`/overview data even when base save-dirty state remains false. The road renderer's legacy `TerrainConformed` marker branch also needs migration. These findings were sent to the terrain/integration owners; this inventory does not claim their fixes have been executed or visually verified.

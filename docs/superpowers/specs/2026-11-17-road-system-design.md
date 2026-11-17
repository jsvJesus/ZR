# New geometric roads

User requests replacing the old system with automobile roads, railway tracks and paths. User selected control points with smooth curves; railway scope is tracks, rails, sleepers and collision. Terrain materials are to be removed later and are untouched here.

Build editable spline meshes independent of old DAT/material data. Automobile roads have pavement, shoulders, thickness and markings; railway has ballast, volumetric sleepers and profiled rails; paths have soil surfaces and sloping edges. Use the same generated triangles for rendering and collision. Store complete definitions in `roads.json`, leaving old files untouched and disconnecting old import/edit machinery.

Editor: three type presets, create/delete roads, Ctrl+LMB append points, LMB select, drag points on terrain, coordinate/height and width controls, insert/delete nodes, save. Esc cancels drawing/dragging. Zero/one-point drafts are saveable. Vegetation clearance uses sampled curves.

## Shared interfaces

Namespace ZoneReborn.World.Roads:
- enum RoadType { Automobile, Railway, Path }.
- RoadNode: float X/Y/Z/HeightOffset; Vector3 Position; Copy().
- RoadDefinition: string Id/Name; RoadType Type; List<RoadNode> Nodes; float Width/ShoulderWidth/Thickness/SurfaceOffset/RailGauge/SleeperSpacing; int LaneCount; bool FollowTerrain; int Revision ignored in JSON; Changed(); static Create(RoadType); static TypeName(RoadType).
- RoadNetwork: List<RoadDefinition> Roads; List<string> Warnings; bool IsDirty/CanSave; long Revision; Changed(RoadDefinition? road = null) increments revisions and dirty.
- RoadVertex: sequential Position Vector3 offset0, Normal Vector3 offset12, Uv Vector2 offset24, Surface float offset32; stride36. Surface IDs: 0 asphalt,1 shoulder,2 soil,3 ballast,4 sleeper,5 rail side,6 rail head.
- RoadSample record struct(Vector3 Position, Vector3 Forward, float Distance, int SegmentIndex).
- RoadSectionMesh record(int SegmentIndex, RoadVertex[] Vertices, int[] Indices, SceneBounds Bounds).
- RoadSpline.Sample(RoadDefinition road, TerrainSurface? surface = null, CancellationToken cancellationToken = default) returns IReadOnlyList<RoadSample>. Following roads use sampled ground + interpolated node HeightOffset + road SurfaceOffset; otherwise interpolated node Y + SurfaceOffset. Distance is cumulative along centerline. Null surface is used for X/Z clearance.
- RoadMesh.Build(RoadDefinition road, TerrainSurface surface, CancellationToken cancellationToken = default) returns IReadOnlyList<RoadSectionMesh>.
- RoadStorage.LoadAsync(string directory, CancellationToken token = default) returns Task<RoadNetwork>; SaveAsync(string directory,RoadNetwork network); Validate(RoadDefinition road).

Editor.Viewport.Roads.RoadRenderer:
- ctor(Device,DeviceContext,AssetPaths,LoadedLevel,TerrainRenderer).
- RoadDefinition? SelectedRoad; int SelectedNodeIndex default -1.
- Render(Vector3 camera,Matrix4x4 viewProjection,float farPlane,bool playing).
- Pick(Vector3 origin,Vector3 direction) returns (RoadDefinition Road,int SegmentIndex)?.
- Keep AdvanceWeatherWetness from existing partial file.
- Track road Revision and terrain HeightRevision/HoleRevision; add/remove GPU resources as network membership changes. Opaque depth-writing geometry, no old decal compositing or DDS material dependencies. Render selected control-point markers separately (never in collision).
- HLSL Roads/Road.hlsl VSMain/PSMain: 36-byte vertex above, Environment lighting include. UV is cumulative distance and signed lateral meters; renderer may pass width/lane count for procedural markings. Railway surface IDs visibly distinguish ballast/wood/metal.

Limits: <=1024 roads, <=256 nodes/road, node coordinates abs<=1e7, width .2..50, shoulder 0..10, thickness .02..2, surface offset -100..100, rail gauge .5..3 (railway width>=gauge+.6), sleeper spacing .3..3, lane count1..8. Adjacent X/Z points >=.25m apart; cap sampler/geometry budgets and reject pathological inputs. Atomic validated save; unreadable document disables save; cancellation propagates. No dependencies, builds, tests, test apps or editor launches. Preserve unrelated edits.

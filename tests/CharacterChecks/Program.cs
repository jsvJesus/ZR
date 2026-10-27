using System.Reflection;
using ZoneReborn.World.Assets;
using System.Numerics;
using ZoneReborn.World.Characters;
using ZoneReborn.World.Characters.Formats;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Storage;
using ZoneReborn.World.Levels.Models;

string root = Path.GetFullPath(args.FirstOrDefault() ?? "bin/packs");
var assembly = typeof(AssetPaths).Assembly;
var loader = assembly.GetType("ZoneReborn.World.Characters.CharacterLoader");
if (loader == null) throw new Exception("FAIL: direct character reader is missing");
try
{
    dynamic asset = loader.GetMethod("Load", [typeof(AssetPaths)])!.Invoke(null, [new AssetPaths(root)])!;
    if (asset.Meshes.Length < 5) throw new Exception("Incomplete composite character");
    int vertices = 0;
    foreach (dynamic mesh in asset.Meshes)
    {
        vertices += mesh.Vertices.Length;
        if (mesh.Indices.Length < 3) throw new Exception("Missing triangles");
        foreach (int index in mesh.Indices)
            if (index < 0 || index >= mesh.Vertices.Length) throw new Exception("Invalid index");
        if (!File.Exists((string)mesh.DiffusePath)) throw new Exception("Missing diffuse texture");
    }
    if (vertices < 10000) throw new Exception("Incomplete geometry");
    if (asset.Animation.Channels.Count != 77) throw new Exception("Idle channels lost");
    Console.WriteLine($"PASS: original character, {asset.Meshes.Length} meshes, {vertices} vertices, 77 animation channels");
}
catch (TargetInvocationException e) { throw e.InnerException!; }

var character = CharacterLoader.Load(new AssetPaths(root));
var pose = new CharacterPose(character);
var exampleRig = new CharacterRig([new SkeletonNode("Bone", -1, Matrix4x4.CreateTranslation(1, 2, 3)),
    new SkeletonNode("BlendBone", 0, Matrix4x4.CreateTranslation(-1, -2, -3))]);
var exampleClip = new SourceAnimation(30, new Dictionary<string, AnimationChannel> { ["Bone"] = new(
    [new(0, Vector3.One)], [new(0, new Vector3(2, 2, 3))], [new(0, Quaternion.Identity)]) });
var exampleMesh = new CharacterMesh([new(new Vector3(1, 1, 0), Vector3.UnitY, Vector2.Zero, 0, 0, 0, Vector3.UnitX)],
    [0, 0, 0], "", exampleRig, [1], false);
var examplePose = new CharacterPose(new CharacterAsset([exampleMesh], exampleClip, 30));
if (Vector3.Distance(examplePose.Vertices[0][0].Position, new Vector3(2, 1, 0)) > 0.00001f)
    throw new Exception("BlendBone inverse bind composition is wrong");
Console.WriteLine("PASS: animated parent and inverse bind compose to expected skinned position");
var first = pose.Vertices.SelectMany(v => v).Select(v => v.Position).ToArray();
var min = first.Aggregate(Vector3.Min); var max = first.Aggregate(Vector3.Max);
if (max.Y - min.Y < 1.5f || max.Y - min.Y > 2.2f || max.X - min.X > 1.5f || max.Z - min.Z > 1.5f)
    throw new Exception($"Invalid animated human bounds {min}..{max}");
pose.Update(1.1);
var second = pose.Vertices.SelectMany(v => v).Select(v => v.Position).ToArray();
if (!second.Any((v) => !float.IsFinite(v.X + v.Y + v.Z)) && first.Zip(second).Max(p => Vector3.Distance(p.First, p.Second)) is > 0.001f and < 0.8f)
    Console.WriteLine($"PASS: animated skin bounds {min}..{max}, feet offset {pose.FloorOffset}");
else throw new Exception("Skin is frozen, non-finite or exploded");

foreach (int length in new[] { 0, 1, 5, 20, 100 })
{
    try { PackedSection.Parse(new byte[length]); throw new Exception("Invalid packed section accepted"); }
    catch (InvalidDataException) { }
}
var surface = assembly.GetType("ZoneReborn.World.Terrain.TerrainSurface");
if (surface == null) throw new Exception("FAIL: Terrain surface query is missing");
float Height(float u, float v) => (float)surface.GetMethod("Interpolate")!.Invoke(null, [0f, 4f, 8f, 16f, u, v])!;
if (MathF.Abs(Height(0.25f, 0.25f) - 3) > 0.0001f || MathF.Abs(Height(0.75f, 0.75f) - 11) > 0.0001f)
    throw new Exception("Terrain interpolation disagrees with rendered triangles");
Console.WriteLine("PASS: malformed containers rejected and Terrain triangle interpolation");

string temporary = Path.Combine(Path.GetTempPath(), "zr-character-check-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporary);
try
{
    var heights = new ushort[256 * 256];
    for (int z = 0; z < 256; z++)
    for (int x = 0; x < 256; x++) heights[z * 256 + x] = (ushort)Math.Max(0, 15 * (4 - z));
    TerrainTileWriter.Write(Path.Combine(temporary, "tile.terrain"), new TerrainTileData(0, 0, heights, new byte[32768], [], []));
    var manifest = new LevelManifest { Terrain = new TerrainDescriptor { SamplesX = 256, SamplesZ = 256, CellSize = 1, HeightScale = 65535 },
        Tiles = [new TerrainTileReference(0, 0, 0, 60, "tile.terrain")] };
    var terrain = new TerrainSurface(new LoadedLevel(temporary, manifest, []));
    if (terrain.TryGetHeight(-1, 10, out _) || terrain.TryGetHeight(float.NaN, 0, out _)) throw new Exception("Invalid spawn coordinates accepted");
    if (!terrain.TryGetHeight(255, 255, out float border) || border != 0) throw new Exception("Last Terrain sample lost");
    var orbit = new ThirdPersonCamera(0);
    var camera = orbit.Position(new Vector3(1, 0, 7.3615f), terrain);
    terrain.TryGetHeight(camera.X, camera.Z, out float ground);
    if (camera.Y < ground + 0.149f) throw new Exception($"Camera endpoint enters Terrain: eye={camera.Y}, ground={ground}");
    Console.WriteLine("PASS: Terrain bounds and steep-slope camera endpoint clearance");
}
finally
{
    string resolved = Path.GetFullPath(temporary);
    string expectedPrefix = Path.Combine(Path.GetFullPath(Path.GetTempPath()), "zr-character-check-");
    if (!resolved.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase)) throw new Exception("Unsafe test cleanup path");
    Directory.Delete(resolved, true);
}

using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
try { CharacterLoader.Load(new AssetPaths(Path.Combine(root, "missing")), cancelled.Token); throw new Exception("Cancelled load accessed resources"); }
catch (OperationCanceledException) { Console.WriteLine("PASS: cancelled character load stops before resource access"); }

byte[] validModel = File.ReadAllBytes(Path.Combine(root, "characters2/basemodel/StalkerBase2.model"));
foreach (int size in new[] { validModel.Length - 1, validModel.Length / 2 })
{
    try { PackedSection.Parse(validModel[..size]); throw new Exception("Truncated model accepted"); }
    catch (InvalidDataException) { }
}
Console.WriteLine("PASS: truncated packed models rejected");

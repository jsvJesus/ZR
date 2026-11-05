using System.Buffers.Binary;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Levels.Storage;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Terrain.Import;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Storage;

namespace ZoneReborn.World.Levels.Import;

internal sealed class FbxTerrainImporter(AssetPaths assets)
{
    public async Task<string> ImportAsync(string name, string[] files, float cellSize,
        IProgress<LevelImportProgress>? progress, CancellationToken token)
    {
        var raster = FbxTerrainRasterizer.Read(files, cellSize, text => progress?.Report(new(text, 0, 1)), token);
        string destination = AssetPathResolver.Resolve(assets.Levels, name);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException($"Уровень «{name}» уже существует.");
        string staging = AssetPathResolver.Resolve(assets.Levels, $".{name}.fbx-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(staging, "terrain", "tiles"));
        try
        {
            float minimum = raster.Heights.Min(), maximum = raster.Heights.Max();
            if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum < -1_000_000 || maximum > 1_000_000
                || !float.IsFinite(maximum - minimum + 256))
                throw new InvalidDataException("Высоты FBX выходят за поддерживаемый диапазон. Проверьте единицы и масштаб земли.");
            var d = new TerrainDescriptor
            {
                SamplesX = raster.Width, SamplesZ = raster.Height, CellSize = cellSize, MeshStep = 1,
                HeightOffset = minimum - 128, HeightScale = Math.Max(1, maximum - minimum + 256),
                HolesFile = "terrain/holes.bin", ImportedFromMesh = true,
                SourceOffsetX = raster.Offset.X, SourceOffsetZ = raster.Offset.Y
            };
            var manifest = new LevelManifest { Name = name, Terrain = d };
            var overview = new TerrainOverviewBuilder(d);
            int total = d.TilesX * d.TilesZ;
            for (int tz = 0; tz < d.TilesZ; tz++)
            for (int tx = 0; tx < d.TilesX; tx++)
            {
                token.ThrowIfCancellationRequested();
                var heights = new ushort[256 * 256];
                for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                    heights[z * 256 + x] = (ushort)Math.Clamp((int)MathF.Round(
                        (raster.Heights[(tz * 256 + z) * raster.Width + tx * 256 + x] - d.HeightOffset) / d.HeightScale * 65535), 0, 65535);
                float Height(int x, int z) => raster.Heights[Math.Clamp(z, 0, d.SamplesZ - 1) * raster.Width + Math.Clamp(x, 0, d.SamplesX - 1)];
                var tile = new TerrainTileData(tx, tz, heights, TerrainNormals.Build(d, tx, tz, Height), [], []);
                string file = $"terrain/tiles/{tx}_{tz}.terrain";
                TerrainTileWriter.Write(AssetPathResolver.Resolve(staging, file), tile); overview.Add(tile);
                manifest.Tiles.Add(new(tx, tz, d.DecodeHeight(heights.Min()), d.DecodeHeight(heights.Max()), file));
                progress?.Report(new(name, manifest.Tiles.Count, total));
            }
            raster.Holes.Write(Path.Combine(staging, "terrain", "holes.bin"));
            TerrainOverviewWriter.Write(Path.Combine(staging, "terrain", "overview.terrain"), overview.Build());
            string diffuse = $"levels/{name}/terrain/ground.dds", normal = $"levels/{name}/terrain/normal.dds";
            Solid(Path.Combine(staging, "terrain", "ground.dds"), 122, 131, 108);
            Solid(Path.Combine(staging, "terrain", "normal.dds"), 128, 128, 255);
            await LevelJson.WriteAsync(Path.Combine(staging, "layers.json"), new[]
            {
                new TerrainLayer { Index = 0, Name = "Земля", DiffuseTexture = diffuse, NormalTexture = normal,
                    ScaleU = d.WorldSizeX / 4, ScaleV = d.WorldSizeZ / 4 }
            }, token);
            int cameraIndex = FindSurface();
            float cx = cameraIndex % raster.Width * cellSize, cz = cameraIndex / raster.Width * cellSize;
            manifest.Camera = new(cx, raster.Heights[cameraIndex] + 60, cz, 0, -0.7f, 40);
            await LevelJson.WriteAsync(Path.Combine(staging, "level.json"), manifest, token);
            string? warning = raster.OverlappingSamples > 0
                ? $"В {raster.OverlappingSamples:N0} точках выбрана верхняя поверхность земли. Проверьте рельеф в этих участках."
                : null;
            await LevelJson.WriteAsync(Path.Combine(staging, "import.json"), new
            {
                Format = "ZoneReborn.FbxTerrainImport", Sources = files.Select(Path.GetFileName).ToArray(), CellSize = cellSize,
                SourceOffsetX = raster.Offset.X, SourceOffsetZ = raster.Offset.Y, raster.Triangles, raster.OverlappingSamples,
                Warnings = warning == null ? Array.Empty<string>() : new[] { warning },
                Note = "Перенесены высоты и отверстия. Верхняя поверхность выбирается при перекрытиях; вертикальные стены и объекты не импортируются."
            }, token);
            token.ThrowIfCancellationRequested(); Directory.Move(staging, destination);
            if (warning != null) progress?.Report(new(name, total, total) { Warning = warning });
            return Path.Combine(destination, "level.json");

            int FindSurface()
            {
                int best = -1; double distance = double.PositiveInfinity;
                for (int z = 0; z < raster.Height - 1; z++)
                for (int x = 0; x < raster.Width - 1; x++)
                {
                    if (raster.Holes.Contains(x, z)) continue;
                    double dx = x - raster.Width * 0.5, dz = z - raster.Height * 0.5, value = dx * dx + dz * dz;
                    if (value < distance) { best = z * raster.Width + x; distance = value; }
                }
                return best >= 0 ? best : throw new InvalidDataException("После переноса не осталось целых ячеек земли. Уменьшите шаг сетки.");
            }
        }
        finally
        {
            string root = Path.GetFullPath(assets.Levels).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(staging).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Некорректный временный каталог импорта.");
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    private static void Solid(string path, byte red, byte green, byte blue)
    {
        var bytes = new byte[132];
        void Put(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value);
        Put(0, 0x20534444); Put(4, 124); Put(8, 0x100F); Put(12, 1); Put(16, 1); Put(20, 4);
        Put(76, 32); Put(80, 0x41); Put(88, 32); Put(92, 255); Put(96, 0xFF00); Put(100, 0xFF0000); Put(104, 0xFF000000); Put(108, 0x1000);
        bytes[128] = red; bytes[129] = green; bytes[130] = blue; bytes[131] = 255;
        using var stream = new FileStream(path, FileMode.CreateNew); stream.Write(bytes);
    }
}

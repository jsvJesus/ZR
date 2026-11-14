using System.Security.Cryptography;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Levels.Storage;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Terrain.Import;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Storage;

namespace ZoneReborn.World.Levels.Import;

public sealed record TerrainUpdateResult(string ManifestPath, string BackupDirectory, int AddedCells,
    int SamplesX, int SamplesZ, bool HeightRangeChanged, IReadOnlyList<string> Warnings);

/// <summary>Builds a new terrain generation, then switches level.json after a complete backup.</summary>
public sealed class FbxTerrainUpdater(AssetPaths assets)
{
    public Task<TerrainUpdateResult> UpdateAsync(LoadedLevel level, string[] files,
        IProgress<LevelImportProgress>? progress, CancellationToken token) =>
        Task.Run(() => UpdateCoreAsync(level, files, progress, token), token);

    private async Task<TerrainUpdateResult> UpdateCoreAsync(LoadedLevel level, string[] files,
        IProgress<LevelImportProgress>? progress, CancellationToken token)
    {
        var old = level.Manifest.Terrain;
        if (!old.ImportedFromMesh) throw new InvalidOperationException("Обновление доступно для Terrain, импортированного из FBX.");
        if (level.TerrainDocument?.IsDirty == true) throw new InvalidOperationException("Сначала сохраните текущие изменения Terrain.");
        if (files.Length == 0) throw new InvalidDataException("Выберите обновлённый FBX земли.");
        string manifestPath = Path.Combine(level.Directory, "level.json");
        byte[] previousManifest = SHA256.HashData(File.ReadAllBytes(manifestPath));
        var raster = FbxTerrainRasterizer.Read(files, old.CellSize, Report, token, old);
        var oldTiles = new Dictionary<(int X, int Z), TerrainTileData>();
        foreach (var reference in level.Manifest.Tiles)
        {
            token.ThrowIfCancellationRequested();
            var tile = TerrainTileReader.Read(AssetPathResolver.Resolve(level.Directory, reference.File));
            if (tile.X != reference.X || tile.Z != reference.Z) throw new InvalidDataException("Некорректные координаты старого тайла Terrain.");
            oldTiles.Add((tile.X, tile.Z), tile);
        }
        var oldHoles = string.IsNullOrWhiteSpace(old.HolesFile) ? new TerrainHoles(old.SamplesX - 1, old.SamplesZ - 1)
            : TerrainHoles.Read(AssetPathResolver.Resolve(level.Directory, old.HolesFile), old.SamplesX - 1, old.SamplesZ - 1);
        Report("Поиск внешних пустых краёв и сохранение старой поверхности");
        var merge = new TerrainExpansion(old, oldTiles, oldHoles, raster, token);
        var holes = new TerrainHoles(raster.Width - 1, raster.Height - 1);
        int added = 0;
        for (int z = 0; z < holes.Height; z++)
        {
            token.ThrowIfCancellationRequested();
            for (int x = 0; x < holes.Width; x++)
            {
                bool preserve = merge.ProtectedCell(x, z);
                bool hole = preserve ? merge.OldHole(x, z) : raster.Holes.Contains(x, z);
                holes.Set(x, z, hole);
                if (!preserve && !hole) added++;
            }
        }
        if (added == 0) throw new InvalidOperationException("В FBX не найдено новой земли за пределами старой поверхности. Текущий Terrain не изменён.");
        string generation = $"terrain/updates/{Guid.NewGuid():N}";
        string output = AssetPathResolver.Resolve(level.Directory, generation);
        float bottom = Math.Min(old.HeightOffset, raster.Heights.Min());
        float top = Math.Max(old.HeightOffset + old.HeightScale, raster.Heights.Max());
        bool rangeChanged = bottom != old.HeightOffset || top > old.HeightOffset + old.HeightScale;
        if (!float.IsFinite(bottom) || !float.IsFinite(top) || bottom < -1_000_000 || top > 1_000_000)
            throw new InvalidDataException("Новые высоты выходят за поддерживаемый диапазон.");
        var descriptor = old with
        {
            SamplesX = raster.Width, SamplesZ = raster.Height,
            OriginX = old.OriginX - merge.OffsetX * old.CellSize, OriginZ = old.OriginZ - merge.OffsetZ * old.CellSize,
            SourceOffsetX = raster.Offset.X, SourceOffsetZ = raster.Offset.Y,
            HeightOffset = rangeChanged ? bottom : old.HeightOffset,
            HeightScale = rangeChanged ? Math.Max(1, top - bottom) : old.HeightScale,
            HolesFile = generation + "/holes.bin", PaintOverviewFile = generation + "/paint-overview.bin",
            HasSourceMaterialLayout = true
        };
        var layers = level.Layers.Select(layer => layer with
        {
            ScaleU = layer.ScaleU * descriptor.SamplesX / old.SamplesX,
            ScaleV = layer.ScaleV * descriptor.SamplesZ / old.SamplesZ
        }).ToList();
        var manifest = new LevelManifest
        {
            Format = level.Manifest.Format, Version = level.Manifest.Version, Name = level.Manifest.Name,
            Terrain = descriptor, LayersFile = generation + "/layers.json", OverviewFile = generation + "/overview.terrain",
            Camera = level.Manifest.Camera
        };
        Report("Перенос материалов новой местности");
        var imported = FbxTerrainMaterials.Read(assets, files, descriptor, layers[0], null, token);
        // Merge a separate palette so a failed update cannot mutate the open level.
        TerrainLayer[] preservedLayers = layers.ToArray();
        new TerrainDocument(level.Directory, manifest, holes, layers).ImportMaterials(imported.Layers);
        for (int index = 0; index < preservedLayers.Length; index++)
            layers[index] = preservedLayers[index] with { SourceMaterial = layers[index].SourceMaterial };
        var materialTiles = FbxTerrainMaterialLayout.Read(files, descriptor, layers, token);
        var heights = new ushort[raster.Heights.Length];
        for (int z = 0; z < descriptor.SamplesZ; z++)
        {
            token.ThrowIfCancellationRequested();
            for (int x = 0; x < descriptor.SamplesX; x++)
            {
                int index = z * descriptor.SamplesX + x;
                if (merge.ProtectedVertex(x, z))
                {
                    ushort previous = merge.OldHeight(x, z);
                    heights[index] = rangeChanged ? Encode(old.DecodeHeight(previous)) : previous;
                }
                else heights[index] = Encode(raster.Heights[index]);
            }
        }
        Report("Резервная копия текущего уровня");
        string backup = Backup(level.Directory, token);
        bool committed = false;
        string temporaryManifest = Path.Combine(level.Directory, $".terrain-update-{Guid.NewGuid():N}.tmp");
        try
        {
            Directory.CreateDirectory(Path.Combine(output, "tiles"));
            var overview = new TerrainOverviewBuilder(descriptor);
            int total = descriptor.TilesX * descriptor.TilesZ;
            for (int tz = 0; tz < descriptor.TilesZ; tz++)
            for (int tx = 0; tx < descriptor.TilesX; tx++)
            {
                token.ThrowIfCancellationRequested();
                var values = new ushort[256 * 256];
                for (int z = 0; z < 256; z++)
                    Array.Copy(heights, (tz * 256 + z) * descriptor.SamplesX + tx * 256, values, z * 256, 256);
                var materials = merge.Materials(tx, tz, materialTiles[(tx, tz)]);
                var tile = new TerrainTileData(tx, tz, values, TerrainNormals.Build(descriptor, tx, tz, Height),
                    materials.ActiveLayers, materials.Masks);
                string file = $"{generation}/tiles/{tx}_{tz}.terrain";
                TerrainTileWriter.Write(AssetPathResolver.Resolve(level.Directory, file), tile); overview.Add(tile);
                manifest.Tiles.Add(new(tx, tz, descriptor.DecodeHeight(values.Min()), descriptor.DecodeHeight(values.Max()), file));
                progress?.Report(new("Запись расширенного Terrain", manifest.Tiles.Count, total));
            }
            holes.Write(AssetPathResolver.Resolve(level.Directory, descriptor.HolesFile));
            TerrainOverviewWriter.Write(AssetPathResolver.Resolve(level.Directory, manifest.OverviewFile), overview.Build());
            await LevelJson.WriteAsync(AssetPathResolver.Resolve(level.Directory, manifest.LayersFile), layers, token);
            var document = new TerrainDocument(level.Directory, manifest, holes, layers);
            if (document.BuildPaintOverview() is { } colors)
            {
                using var writer = new BinaryWriter(File.Open(AssetPathResolver.Resolve(level.Directory, descriptor.PaintOverviewFile), FileMode.CreateNew));
                writer.Write("ZRPAINT1"u8); writer.Write(document.PaintOverviewWidth); writer.Write(document.PaintOverviewHeight); writer.Write(colors);
            }
            await LevelJson.WriteAsync(Path.Combine(output, "update.json"), new
            {
                Sources = files, BackupDirectory = backup, AddedCells = added, PreviousSamplesX = old.SamplesX,
                PreviousSamplesZ = old.SamplesZ, descriptor.SamplesX, descriptor.SamplesZ, HeightRangeChanged = rangeChanged,
                Warnings = imported.Warnings
            }, token);
            await LevelJson.WriteAsync(temporaryManifest, manifest, token);
            token.ThrowIfCancellationRequested();
            if (!SHA256.HashData(File.ReadAllBytes(manifestPath)).AsSpan().SequenceEqual(previousManifest))
                throw new IOException("Уровень изменился во время обновления. Новый Terrain не применён; повторите операцию.");
            File.Move(temporaryManifest, manifestPath, true);
            committed = true;
            return new(manifestPath, backup, added, descriptor.SamplesX, descriptor.SamplesZ, rangeChanged, imported.Warnings);
        }
        finally
        {
            if (File.Exists(temporaryManifest)) File.Delete(temporaryManifest);
            if (!committed && Directory.Exists(output))
            {
                EnsureChild(level.Directory, output);
                try { Directory.Delete(output, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        ushort Encode(float value) => (ushort)Math.Clamp((int)MathF.Round((value - descriptor.HeightOffset) / descriptor.HeightScale * 65535), 0, 65535);
        float Height(int x, int z) => descriptor.DecodeHeight(heights[Math.Clamp(z, 0, descriptor.SamplesZ - 1)
            * descriptor.SamplesX + Math.Clamp(x, 0, descriptor.SamplesX - 1)]);
        void Report(string text) => progress?.Report(new(text, 0, 1));
    }

    private string Backup(string directory, CancellationToken token)
    {
        string root = Path.Combine(assets.Root, "backups", "levels", new DirectoryInfo(directory).Name);
        string name = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
        string destination = AssetPathResolver.Resolve(root, name), partial = destination + ".partial";
        // Backups must never be traversed as part of their own input.
        string sourceRoot = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (Path.GetFullPath(partial).StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Резервная копия не может находиться внутри исходного уровня.");
        Directory.CreateDirectory(partial);
        try
        {
            foreach (string file in Directory.EnumerateFiles(directory, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true, IgnoreInaccessible = false, AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                token.ThrowIfCancellationRequested();
                string target = AssetPathResolver.Resolve(partial, Path.GetRelativePath(directory, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target, false);
            }
            token.ThrowIfCancellationRequested(); Directory.Move(partial, destination); return destination;
        }
        catch (Exception error)
        {
            if (Directory.Exists(partial))
            {
                EnsureChild(root, partial);
                try { Directory.Delete(partial, true); }
                catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
                {
                    throw new IOException($"Не удалось удалить незавершённую резервную копию: {partial}", new AggregateException(error, cleanup));
                }
            }
            throw;
        }
    }

    private static void EnsureChild(string directory, string path)
    {
        string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Временные файлы Terrain вышли за пределы уровня.");
    }
}

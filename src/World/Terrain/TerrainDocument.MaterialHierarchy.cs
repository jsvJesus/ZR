using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    public IReadOnlyList<TerrainLayer> GetMaterialHierarchy()
    {
        return layers
            .OrderBy(GetPaintOrder)
            .ThenBy(layer => layer.Index)
            .ToArray();
    }

    public void MoveMaterial(int layerIndex, int direction)
    {
        if (direction != -1 && direction != 1)
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        if (layerIndex == 0)
        {
            throw new InvalidOperationException(
                "Базовый материал всегда находится внизу.");
        }

        var ordered = GetMaterialHierarchy()
            .Where(layer => layer.Index != 0)
            .ToList();

        var visible = ordered
            .Where(layer => !layer.IsArchived)
            .ToArray();

        int position = Array.FindIndex(
            visible,
            layer => layer.Index == layerIndex);

        if (position < 0)
        {
            throw new InvalidOperationException(
                "Материал не найден в иерархии.");
        }

        int destination = position + direction;

        if (destination < 0 || destination >= visible.Length)
        {
            return;
        }

        int firstIndex = ordered.FindIndex(
            layer => layer.Index == visible[position].Index);

        int secondIndex = ordered.FindIndex(
            layer => layer.Index == visible[destination].Index);

        (ordered[firstIndex], ordered[secondIndex]) =
            (ordered[secondIndex], ordered[firstIndex]);

        for (int index = 0; index < ordered.Count; index++)
        {
            var layer = ordered[index];

            layers[layer.Index] = layer with
            {
                PaintOrder = index
            };
        }

        MarkMaterialCatalogChanged();
    }

    public void SetMaterialLocked(int layerIndex, bool locked)
    {
        if (layerIndex == 0)
        {
            throw new InvalidOperationException(
                "Базовый материал Terrain блокировать нельзя.");
        }

        var layer = RequireEditableMaterial(layerIndex);

        if (layer.IsPaintLocked == locked)
        {
            return;
        }

        SetLayer(layer with
        {
            IsPaintLocked = locked
        });
    }

    private static int GetPaintOrder(TerrainLayer layer)
    {
        if (layer.Index == 0)
        {
            return int.MaxValue;
        }

        return layer.PaintOrder ?? layer.Index;
    }
}

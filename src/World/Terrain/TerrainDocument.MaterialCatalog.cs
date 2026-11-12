using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    public TerrainLayer AddMaterial()
    {
        if (layers.Count >= 255)
        {
            throw new InvalidOperationException(
                "Terrain поддерживает не более 255 материалов.");
        }

        int number = 1;
        string name;

        do
        {
            name = $"Материал {number++}";
        }
        while (layers.Any(layer =>
            !layer.IsArchived &&
            string.Equals(
                layer.Name,
                name,
                StringComparison.OrdinalIgnoreCase)));

        var layer = new TerrainLayer
        {
            Index = layers.Count,
            Name = name,
            NeedsTexture = true,
            PaintOrder = layers
                .Where(layer => layer.Index != 0)
                .Select(layer => layer.PaintOrder ?? layer.Index)
                .DefaultIfEmpty(-1)
                .Max() + 1,
            ScaleU = Descriptor.SamplesX * Descriptor.CellSize / 4f,
            ScaleV = Descriptor.SamplesZ * Descriptor.CellSize / 4f
        };

        layers.Add(layer);

        MarkMaterialCatalogChanged();

        return layer;
    }

    public void UpdateMaterial(TerrainLayer updated)
    {
        var current = RequireMaterial(updated.Index);

        if (current.IsArchived)
        {
            throw new InvalidOperationException(
                "Архивный материал сначала необходимо восстановить.");
        }

        string name = updated.Name.Trim();

        if (name.Length == 0 || name.Length > 128)
        {
            throw new InvalidOperationException(
                "Название материала должно содержать от 1 до 128 символов.");
        }

        if (layers.Any(layer =>
            layer.Index != updated.Index &&
            !layer.IsArchived &&
            string.Equals(
                layer.Name,
                name,
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Материал с таким названием уже существует.");
        }

        var physics = updated.Settings.Physics;
        var visual = updated.Settings.Visual;

        CheckRange(physics.Friction, 0, 3, "Трение");
        CheckRange(physics.Hardness, 0, 1, "Твёрдость");
        CheckRange(visual.Roughness, 0, 1, "Roughness");
        CheckRange(visual.Metallic, 0, 1, "Metallic");
        CheckRange(visual.Specular, 0, 1, "Specular");
        CheckRange(visual.Opacity, 0, 1, "Opacity");

        var blend = updated.Settings.Blend;

        CheckRange(blend.NoiseIntensity, 0, 1, "Noise Blend");
        CheckRange(blend.HeightIntensity, 0, 1, "Height Blend");
        CheckRange(blend.SlopeIntensity, 0, 1, "Slope Blend");
        CheckRange(blend.MacroIntensity, 0, 1, "Macro Variation");

        CheckRange(
            blend.MacroScaleMeters,
            1,
            1024,
            "Macro Scale");

        var weather = updated.Settings.Weather;

        CheckRange(weather.SoakSeconds, 1, 3600, "Время намокания");
        CheckRange(weather.DrySeconds, 1, 7200, "Время высыхания");
        CheckRange(weather.Darkening, 0, 1, "Потемнение");
        CheckRange(weather.WetRoughness, 0.045f, 1, "Мокрый Roughness");
        CheckRange(weather.WetSpecular, 0, 1, "Мокрый Specular");
        CheckRange(weather.Patchiness, 0, 1, "Влажные пятна");
        CheckRange(weather.PatchScaleMeters, 1, 512, "Масштаб пятен");
        CheckRange(weather.PuddleStrength, 0, 1, "Лужицы");
        CheckRange(weather.WetFriction, 0, 3, "Мокрое трение");

        SetLayer(updated with
        {
            Name = name,
            SourceMaterial = current.SourceMaterial,
            MaterialType = current.MaterialType,
            TextureVariants = updated.GetTextureVariants()
        });
    }

    public void ArchiveMaterial(int index)
    {
        if (index == 0)
        {
            throw new InvalidOperationException(
                "Базовый материал Terrain удалять нельзя.");
        }

        var layer = RequireMaterial(index);

        if (layer.IsArchived)
        {
            return;
        }

        SetLayer(layer with { IsArchived = true });
    }

    public void RestoreMaterial(int index)
    {
        var layer = RequireMaterial(index);

        if (!layer.IsArchived)
        {
            return;
        }

        string name = layer.Name;
        int suffix = 2;

        while (layers.Any(other =>
            other.Index != index &&
            !other.IsArchived &&
            string.Equals(
                other.Name,
                name,
                StringComparison.OrdinalIgnoreCase)))
        {
            name = $"{layer.Name} ({suffix++})";
        }

        SetLayer(layer with
        {
            Name = name,
            IsArchived = false
        });
    }

    public int AddTextureVariant(int layerIndex)
    {
        var layer = RequireEditableMaterial(layerIndex);
        var variants = layer.GetTextureVariants();

        if (variants.Count >= 4)
        {
            throw new InvalidOperationException(
                "Текущий PBR-рендер поддерживает до 4 покрытий материала.");
        }

        if (layer.TextureVariants is not { Count: > 0 })
        {
            float meters = Descriptor.SamplesX *
                Descriptor.CellSize / layer.ScaleU;

            if (!float.IsFinite(meters) || meters <= 0)
            {
                meters = 4f;
            }

            variants[0] = variants[0] with
            {
                TilingMeters = Math.Clamp(
                    meters,
                    0.05f,
                    10000f)
            };
        }

        int index = variants.Count;

        variants.Add(new TerrainTextureVariant
        {
            Name = $"Покрытие {index + 1}"
        });

        SetLayer(layer with
        {
            TextureVariants = variants
        });

        return index;
    }

    public void UpdateTextureVariant(
        int layerIndex,
        int variantIndex,
        TerrainTextureVariant variant,
        float[]? averageColor = null)
    {
        var layer = RequireEditableMaterial(layerIndex);
        var variants = layer.GetTextureVariants();

        if (variantIndex < 0 || variantIndex >= variants.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(variantIndex));
        }

        if (string.IsNullOrWhiteSpace(variant.Name))
        {
            throw new InvalidOperationException(
                "Укажите название покрытия.");
        }

        CheckRange(
            variant.TilingMeters,
            0.05f,
            10000f,
            "Размер покрытия");

        CheckRange(variant.SlopeBias, -1, 1, "Slope Bias");

        variants[variantIndex] = variant with
        {
            Name = variant.Name.Trim()
        };

        var updated = layer with
        {
            TextureVariants = variants
        };

        if (variantIndex == 0)
        {
            float scaleU = Descriptor.SamplesX *
                Descriptor.CellSize / variant.TilingMeters;

            float scaleV = Descriptor.SamplesZ *
                Descriptor.CellSize / variant.TilingMeters;

            updated = updated with
            {
                DiffuseTexture = variant.AlbedoTexture,
                NormalTexture = variant.NormalTexture,
                NeedsTexture = string.IsNullOrWhiteSpace(
                    variant.AlbedoTexture),
                AverageColor = averageColor ?? layer.AverageColor,
                ScaleU = scaleU,
                ScaleV = scaleV
            };
        }

        SetLayer(updated);
    }

    public void RemoveTextureVariant(
        int layerIndex,
        int variantIndex)
    {
        var layer = RequireEditableMaterial(layerIndex);
        var variants = layer.GetTextureVariants();

        if (variantIndex <= 0 || variantIndex >= variants.Count)
        {
            throw new InvalidOperationException(
                "Основное покрытие удалять нельзя.");
        }

        variants.RemoveAt(variantIndex);

        SetLayer(layer with
        {
            TextureVariants = variants
        });
    }

    private TerrainLayer RequireMaterial(int index)
    {
        if (index < 0 || index >= layers.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return layers[index];
    }

    private TerrainLayer RequireEditableMaterial(int index)
    {
        var layer = RequireMaterial(index);

        if (layer.IsArchived)
        {
            throw new InvalidOperationException(
                "Архивный материал недоступен для редактирования.");
        }

        return layer;
    }

    private static void CheckRange(
        float value,
        float minimum,
        float maximum,
        string name)
    {
        if (!float.IsFinite(value) ||
            value < minimum ||
            value > maximum)
        {
            throw new InvalidOperationException(
                $"{name}: допустимый диапазон {minimum}–{maximum}.");
        }
    }

    private void MarkMaterialCatalogChanged()
    {
        Revision++;
        MaterialRevision++;
        InvalidatePaintOverview();
    }
}

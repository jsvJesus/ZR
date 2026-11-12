using System.Globalization;
using System.IO;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Terrain.Import;
using ZoneReborn.World.Terrain.Models;

namespace Editor.Terrain;

public sealed partial class TerrainEditorPanel
{
    private readonly TextBox _catalogName = new();

    private readonly ComboBox _catalogSurface = new()
    {
        ItemsSource = new[]
        {
            "Земля",
            "Песок",
            "Гравий",
            "Асфальт",
            "Дерево",
            "Камень",
            "Вода",
            "Стекло"
        },
        SelectedIndex = 0
    };

    private readonly TextBox _catalogFriction = new();
    private readonly TextBox _catalogHardness = new();
    private readonly TextBox _catalogFootsteps = new();
    private readonly TextBox _catalogRoughness = new();
    private readonly TextBox _catalogMetallic = new();
    private readonly TextBox _catalogSpecular = new();
    private readonly TextBox _catalogOpacity = new();

    private readonly ComboBox _catalogVariants = new();

    private readonly TextBox _catalogVariantName = new();
    private readonly TextBox _catalogVariantScale = new();

    private int _selectedVariantIndex;
    private int _selectedVariantLayer = -1;
    private bool _restoringVariantSelection;

    private readonly ComboBox _catalogTextureChannel = new()
    {
        ItemsSource = new[]
        {
            "Albedo",
            "Normal",
            "Roughness",
            "Height"
        },
        SelectedIndex = 0
    };

    private readonly TextBlock _catalogTextureInfo = new()
    {
        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        FontSize = 12
    };

    private readonly ComboBox _catalogArchived = new();

    private void BuildMaterialCatalogControls()
    {
        var properties = new StackPanel { Spacing = 6 };

        AddField(properties, "Название материала", _catalogName);
        AddField(properties, "Тип поверхности", _catalogSurface);
        AddField(properties, "Трение (0–3)", _catalogFriction);
        AddField(properties, "Твёрдость (0–1)", _catalogHardness);
        AddField(properties, "Звук шагов", _catalogFootsteps);
        AddField(properties, "Roughness (0–1)", _catalogRoughness);
        AddField(properties, "Metallic (0–1)", _catalogMetallic);
        AddField(properties, "Specular (0–1)", _catalogSpecular);
        AddField(properties, "Opacity (0–1)", _catalogOpacity);

        AddAction(properties, "Добавить материал", AddCatalogMaterial);
        AddAction(properties, "Сохранить свойства", ApplyCatalogMaterial);
        AddAction(properties, "Удалить из каталога", ArchiveCatalogMaterial);

        properties.Children.Add(new TextBlock
        {
            Text = "Архив материалов",
            FontWeight = Avalonia.Media.FontWeight.Bold
        });

        properties.Children.Add(_catalogArchived);

        AddAction(
            properties,
            "Восстановить из архива",
            RestoreCatalogMaterial);

        _materialControls.Children.Add(new Expander
        {
            Header = "Свойства и управление материалами",
            IsExpanded = true,
            Content = properties
        });

        var textures = new StackPanel { Spacing = 6 };

        AddField(textures, "Покрытия материала", _catalogVariants);
        AddField(textures, "Название покрытия", _catalogVariantName);
        AddField(textures, "Размер повторения, м", _catalogVariantScale);

        textures.Children.Add(_catalogTextureInfo);

        AddAction(textures, "Добавить покрытие", AddCatalogVariant);
        AddAction(textures, "Сохранить покрытие", ApplyCatalogVariant);
        AddAction(textures, "Удалить покрытие", RemoveCatalogVariant);

        AddField(textures, "Текстурный канал", _catalogTextureChannel);

        var assignTexture = new Button
        {
            Content = "Назначить текстуру в выбранный канал"
        };

        assignTexture.Click += (_, _) =>
        {
            if (Active)
            {
                _materialTask = MaterialOperationAsync(
                    AssignCatalogTextureAsync);
            }
        };

        textures.Children.Add(assignTexture);

        _materialControls.Children.Add(new Expander
        {
            Header = "Дополнительные текстурные покрытия",
            IsExpanded = false,
            Content = textures
        });

        _palette.SelectionChanged += (_, _) =>
        {
            if (_palette.SelectedItem is not TerrainLayer layer)
            {
                return;
            }

            if (_selectedVariantLayer != layer.Index)
            {
                _selectedVariantLayer = layer.Index;
                _selectedVariantIndex = 0;
            }

            RefreshMaterialCatalog();
        };

        _catalogVariants.SelectionChanged += (_, _) =>
        {
            if (_restoringVariantSelection ||
                _catalogVariants.SelectedIndex < 0)
            {
                return;
            }

            _selectedVariantIndex = _catalogVariants.SelectedIndex;
            RefreshCatalogVariant();
        };

        RefreshMaterialCatalog();
    }

    private static void AddField(
        StackPanel parent,
        string name,
        Control control)
    {
        parent.Children.Add(new TextBlock { Text = name });
        parent.Children.Add(control);
    }

    private void AddAction(
        StackPanel parent,
        string title,
        Action action)
    {
        var button = new Button { Content = title };

        button.Click += (_, _) =>
        {
            if (!Active)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception error)
            {
                _status.Text = error.Message;
            }
        };

        parent.Children.Add(button);
    }

    private void RefreshMaterialCatalog(int? variantIndex = null)
    {
        _catalogArchived.ItemsSource = _level?.Layers
            .Where(layer => layer.IsArchived)
            .ToArray() ?? [];

        if (_palette.SelectedItem is not TerrainLayer layer)
        {
            _restoringVariantSelection = true;

            try
            {
                _catalogVariants.ItemsSource =
                    Array.Empty<TerrainTextureVariant>();
            }
            finally
            {
                _restoringVariantSelection = false;
            }

            return;
        }

        if (_selectedVariantLayer != layer.Index)
        {
            _selectedVariantLayer = layer.Index;
            _selectedVariantIndex = 0;
        }

        var settings = layer.Settings;

        _catalogName.Text = layer.Name;
        _catalogSurface.SelectedIndex = (int)settings.SurfaceType;

        _catalogFriction.Text = Format(settings.Physics.Friction);
        _catalogHardness.Text = Format(settings.Physics.Hardness);
        _catalogFootsteps.Text = settings.Physics.FootstepSound;

        _catalogRoughness.Text = Format(settings.Visual.Roughness);
        _catalogMetallic.Text = Format(settings.Visual.Metallic);
        _catalogSpecular.Text = Format(settings.Visual.Specular);
        _catalogOpacity.Text = Format(settings.Visual.Opacity);

        var variants = layer.GetTextureVariants();

        int selected = Math.Clamp(
            variantIndex ?? _selectedVariantIndex,
            0,
            variants.Count - 1);

        _selectedVariantIndex = selected;

        _restoringVariantSelection = true;

        try
        {
            _catalogVariants.ItemsSource = variants;
            _catalogVariants.SelectedIndex = selected;
        }
        finally
        {
            _restoringVariantSelection = false;
        }

        RefreshCatalogVariant();
    }

    private void RefreshCatalogVariant()
    {
        if (_catalogVariants.SelectedItem is not
            TerrainTextureVariant variant)
        {
            _catalogTextureInfo.Text = "";
            return;
        }

        _catalogVariantName.Text = variant.Name;
        _catalogVariantScale.Text = Format(variant.TilingMeters);

        _catalogTextureInfo.Text =
            $"Albedo: {FileName(variant.AlbedoTexture)}\n" +
            $"Normal: {FileName(variant.NormalTexture)}\n" +
            $"Roughness: {FileName(variant.RoughnessTexture)}\n" +
            $"Height: {FileName(variant.HeightTexture)}";
    }

    private void AddCatalogMaterial()
    {
        if (_level?.TerrainDocument is not { } document)
        {
            return;
        }

        var layer = document.AddMaterial();

        RefreshPalette(layer.Index);
        RefreshMode();

        _status.Text = "Материал создан. Назначьте ему текстуру.";
    }

    private void ApplyCatalogMaterial()
    {
        if (_level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        var settings = layer.Settings with
        {
            SurfaceType = (TerrainSurfaceType)_catalogSurface.SelectedIndex,

            Physics = new TerrainPhysicalProperties
            {
                Friction = Parse(_catalogFriction, 0, 3, "Трение"),
                Hardness = Parse(_catalogHardness, 0, 1, "Твёрдость"),
                FootstepSound = (_catalogFootsteps.Text ?? "").Trim()
            },

            Visual = new TerrainVisualProperties
            {
                Roughness = Parse(_catalogRoughness, 0, 1, "Roughness"),
                Metallic = Parse(_catalogMetallic, 0, 1, "Metallic"),
                Specular = Parse(_catalogSpecular, 0, 1, "Specular"),
                Opacity = Parse(_catalogOpacity, 0, 1, "Opacity")
            }
        };

        document.UpdateMaterial(layer with
        {
            Name = _catalogName.Text ?? "",
            Settings = settings
        });

        RefreshPalette(layer.Index);
        RefreshMode();

        _status.Text = "Свойства материала обновлены.";
    }

    private void ArchiveCatalogMaterial()
    {
        if (_level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        document.ArchiveMaterial(layer.Index);

        RefreshPalette(0);
        RefreshMode();

        _status.Text =
            "Материал перемещён в архив. Маски Terrain сохранены.";
    }

    private void RestoreCatalogMaterial()
    {
        if (_level?.TerrainDocument is not { } document ||
            _catalogArchived.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        document.RestoreMaterial(layer.Index);

        RefreshPalette(layer.Index);
        RefreshMode();

        _status.Text = "Материал восстановлен.";
    }

    private void AddCatalogVariant()
    {
        if (_level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        int index = document.AddTextureVariant(layer.Index);

        RefreshPalette(layer.Index);
        RefreshMaterialCatalog(index);
        RefreshMode();

        _status.Text = "Покрытие добавлено.";
    }

    private void ApplyCatalogVariant()
    {
        if (_level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        int index = _catalogVariants.SelectedIndex;
        var variants = layer.GetTextureVariants();

        if (index < 0 || index >= variants.Count)
        {
            return;
        }

        var variant = variants[index] with
        {
            Name = _catalogVariantName.Text ?? "",
            TilingMeters = Parse(
                _catalogVariantScale,
                0.05f,
                10000f,
                "Размер покрытия")
        };

        document.UpdateTextureVariant(
            layer.Index,
            index,
            variant);

        RefreshPalette(layer.Index);
        RefreshMaterialCatalog(index);
        RefreshMode();

        _status.Text = "Настройки покрытия обновлены.";
    }

    private void RemoveCatalogVariant()
    {
        if (_level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        int index = _catalogVariants.SelectedIndex;

        document.RemoveTextureVariant(layer.Index, index);

        RefreshPalette(layer.Index);
        RefreshMaterialCatalog(Math.Max(0, index - 1));
        RefreshMode();

        _status.Text = "Дополнительное покрытие удалено.";
    }

    private async Task AssignCatalogTextureAsync(
        CancellationToken token)
    {
        if (_assets == null ||
            _level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        int variantIndex = _catalogVariants.SelectedIndex;
        int channel = _catalogTextureChannel.SelectedIndex;

        var variants = layer.GetTextureVariants();

        if (variantIndex < 0 || variantIndex >= variants.Count ||
            channel < 0 || channel > 3)
        {
            return;
        }

        if (variantIndex == 0 &&
            channel == 0 &&
            document.Descriptor.ImportedFromMesh &&
            !document.Descriptor.HasSourceMaterialLayout &&
            !string.IsNullOrEmpty(layer.SourceMaterial))
        {
            throw new InvalidOperationException(
                "Сначала восстановите участки материалов из исходного FBX.");
        }

        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;

        if (storage == null)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new()
        {
            Title = "Выбрать текстуру покрытия",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Изображения")
                {
                    Patterns =
                    [
                        "*.dds",
                        "*.tga",
                        "*.png",
                        "*.jpg",
                        "*.jpeg",
                        "*.bmp"
                    ]
                }
            ]
        });

        token.ThrowIfCancellationRequested();

        if (files.FirstOrDefault()?.TryGetLocalPath() is not { } path)
        {
            return;
        }

        string texture = await Task.Run(
            () => FbxTerrainMaterials.CopyTexture(_assets, path, token),
            token);

        token.ThrowIfCancellationRequested();

        float[]? averageColor = null;

        if (variantIndex == 0 && channel == 0)
        {
            averageColor = await Task.Run(
                () => TerrainMaterialColor.Read(
                    AssetPathResolver.Resolve(_assets.Root, texture)),
                token);
        }

        token.ThrowIfCancellationRequested();

        var variant = variants[variantIndex];

        variant = channel switch
        {
            0 => variant with { AlbedoTexture = texture },
            1 => variant with { NormalTexture = texture },
            2 => variant with { RoughnessTexture = texture },
            3 => variant with { HeightTexture = texture },
            _ => variant
        };

        document.UpdateTextureVariant(
            layer.Index,
            variantIndex,
            variant,
            averageColor);

        RefreshPalette(layer.Index);
        RefreshMaterialCatalog(variantIndex);

        _status.Text = "Текстура покрытия назначена. Сохраните Terrain.";
    }

    private static string Format(float value)
    {
        return value.ToString("G6", CultureInfo.InvariantCulture);
    }

    private static string FileName(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? "—"
            : Path.GetFileName(path);
    }

    private static float Parse(
        TextBox input,
        float minimum,
        float maximum,
        string name)
    {
        string text = (input.Text ?? "").Replace(',', '.');

        if (!float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float value) ||
            !float.IsFinite(value) ||
            value < minimum ||
            value > maximum)
        {
            throw new InvalidOperationException(
                $"{name}: укажите значение от {minimum} до {maximum}.");
        }

        return value;
    }
}

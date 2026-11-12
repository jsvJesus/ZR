using System.Globalization;
using System.IO;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Terrain.Import;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain;

namespace Editor.Terrain;

public sealed partial class TerrainEditorPanel
{
    private AssetPaths? _assets;
    private readonly StackPanel _materialControls = new() { Spacing = 6, IsVisible = false };
    private readonly ComboBox _palette = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBox _repeat = new() { Text = "4" };
    private readonly TextBlock _textureInfo = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly TextBlock _folderInfo = new() { Text = "Поиск рядом с FBX", TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private CancellationTokenSource? _materialCancellation;
    private Task _materialTask = Task.CompletedTask;
    private string? _textureFolder;

    public void CancelMaterialImport() => _materialCancellation?.Cancel();

    public void SetPainting(bool showMaterials)
    {
        _viewport?.EndTerrainPaint();

        _materialsSection = showMaterials;

        RefreshMode();
    }

    private void BuildMaterialControls(StackPanel root)
    {
        _materialControls.Children.Add(new TextBlock { Text = "Материал земли" });
        _materialControls.Children.Add(new TextBlock
        {
            Text = "Выберите материал и назначьте изображение — оно появится на всех его участках из FBX.",
            TextWrapping = TextWrapping.Wrap, FontSize = 12
        });
        _materialControls.Children.Add(_palette);
        _materialControls.Children.Add(_textureInfo);
        Add("Импортировать материалы из FBX", ImportPaletteAsync);
        Add("Восстановить участки из FBX", RestoreSourceLayoutAsync);
        Add("Папка поиска текстур…", ChooseTextureFolderAsync);
        _materialControls.Children.Add(_folderInfo);
        Add("Выбрать текстуру материала…", token => AssignTextureAsync(false, token));
        Add("Выбрать normal map…", token => AssignTextureAsync(true, token));
        _materialControls.Children.Add(new TextBlock { Text = "Размер повторения текстуры, м" });
        _materialControls.Children.Add(_repeat);

        var apply = new Button { Content = "Применить размер" };
        apply.Click += (_, _) =>
        {
            if (!Active || _palette.SelectedItem is not TerrainLayer layer || _level?.TerrainDocument is not { } document) return;

            if (!float.TryParse((_repeat.Text ?? "").Replace(',', '.'), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float meters)
                || !float.IsFinite(meters) || meters < 0.05f || meters > 10000)
            {
                _status.Text = "Размер повторения: от 0.05 до 10000 метров."; return;
            }
            var d = document.Descriptor;

            var updated = layer with
            {
                ScaleU = d.SamplesX * d.CellSize / meters,
                ScaleV = d.SamplesZ * d.CellSize / meters
            };

            if (updated.TextureVariants is { Count: > 0 })
            {
                var variants = updated.GetTextureVariants();

                variants[0] = variants[0] with
                {
                    TilingMeters = meters
                };

                updated = updated with
                {
                    TextureVariants = variants
                };
            }

            document.SetLayer(updated);
            RefreshPalette(layer.Index); RefreshMode();
        };

        _materialControls.Children.Add(apply);
        _palette.SelectionChanged += (_, _) =>
        {
            _viewport?.EndTerrainPaint();
            if (_palette.SelectedItem is not TerrainLayer layer) return;
            _textureInfo.Text = layer.NeedsTexture ? "В FBX нет доступного изображения. Выберите текстуру материала."
                : Path.GetFileName(layer.DiffuseTexture);
            if (_level != null) _repeat.Text = (_level.Manifest.Terrain.SamplesX * _level.Manifest.Terrain.CellSize / layer.ScaleU)
                .ToString("G4", CultureInfo.InvariantCulture);
        };
        root.Children.Add(_materialControls);
        BuildMaterialCatalogControls();
        BuildPbrControls();
        BuildWeatherMaterialControls();
        BuildMaterialHierarchyControls();

        void Add(string title, Func<CancellationToken, Task> operation)
        {
            var button = new Button { Content = title };
            button.Click += (_, _) => { if (Active) _materialTask = MaterialOperationAsync(operation); };
            _materialControls.Children.Add(button);
        }
    }

    private void RefreshPalette(int? selected = null)
    {
        int index = selected ??
                    (_palette.SelectedItem as TerrainLayer)?.Index ?? 0;

        TerrainLayer[] visible = _level?.TerrainDocument is { } document
            ? document.GetMaterialHierarchy()
                .Where(layer => !layer.IsArchived)
                .ToArray()
            : [];

        _palette.ItemsSource = visible;

        _palette.SelectedItem = visible.FirstOrDefault(
                                    layer => layer.Index == index) ??
                                visible.FirstOrDefault();

        RefreshMaterialHierarchy();
    }

    private async Task MaterialOperationAsync(Func<CancellationToken, Task> operation)
    {
        _viewport?.EndTerrainPaint();
        using var cancellation = new CancellationTokenSource();
        _materialCancellation = cancellation; BusyChanged?.Invoke(); RefreshMode();
        string? message = null;
        try { await operation(cancellation.Token); message = _status.Text; }
        catch (OperationCanceledException) { message = "Импорт материалов отменён."; }
        catch (Exception error) { message = error.Message; }
        finally
        {
            _materialCancellation = null; BusyChanged?.Invoke(); RefreshPalette(); RefreshMode();
            if (message != null) _status.Text = message;
        }
    }

    private async Task ChooseTextureFolderAsync(CancellationToken token)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage == null) return;
        var folders = await storage.OpenFolderPickerAsync(new() { Title = "Папка с текстурами FBX", AllowMultiple = false });
        token.ThrowIfCancellationRequested();
        if (folders.FirstOrDefault()?.TryGetLocalPath() is { } folder) { _textureFolder = folder; _folderInfo.Text = folder; }
    }

    private async Task ImportPaletteAsync(CancellationToken token)
    {
        if (_level?.TerrainDocument is not { } document || _assets == null) return;
        string[] files = SourceFiles();
        if (files.Length == 0)
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storage == null) return;
            var selected = await storage.OpenFilePickerAsync(new()
            {
                Title = "FBX с материалами земли", AllowMultiple = true,
                FileTypeFilter = [new FilePickerFileType("FBX") { Patterns = ["*.fbx"] }]
            });
            files = selected.Select(value => value.TryGetLocalPath()).OfType<string>().ToArray();
        }
        token.ThrowIfCancellationRequested(); if (files.Length == 0) return;
        _status.Text = "Чтение материалов и перенос текстур…";
        var result = await Task.Run(() => FbxTerrainMaterials.Read(_assets, files, document.Descriptor, document.Layers[0], _textureFolder, token), token);
        token.ThrowIfCancellationRequested(); document.ImportMaterials(result.Layers);
        if (document.Descriptor.ImportedFromMesh && !document.Descriptor.HasSourceMaterialLayout)
            await ApplySourceLayoutAsync(files, token);
        int missing = document.Layers.Count(layer => layer.NeedsTexture);
        _status.Text = $"Материалов из FBX: {result.Layers.Count}. " + (missing > 0
            ? $"Нужно назначить изображения: {missing}. Выберите материал и нажмите «Выбрать текстуру материала»."
            : "Назначьте изображение материалу — оно заменится на всех его участках.");
        if (result.Warnings.Count > 0) _status.Text += "\n" + string.Join("\n", result.Warnings.Take(3));
    }

    private string[] SourceFiles()
    {
        if (_level == null || _assets == null) return [];
        string source = AssetPathResolver.Resolve(_assets.ImportLevels, _level.Manifest.Name);
        if (Directory.Exists(source))
        {
            string[] files = Directory.EnumerateFiles(source, "*", SearchOption.TopDirectoryOnly)
                .Where(file => Path.GetExtension(file).Equals(".fbx", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (files.Length > 0) return files;
        }
        return _level.Layers.Select(layer => layer.SourceMaterial)
            .Where(value => !string.IsNullOrEmpty(value) && value.LastIndexOf('|') > 0)
            .Select(value => value[..value.LastIndexOf('|')]).Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private async Task ApplySourceLayoutAsync(string[] files, CancellationToken token)
    {
        if (_level?.TerrainDocument is not { } document) return;
        _status.Text = "Восстановление участков материалов по исходному FBX…";
        TerrainLayer[] palette = document.Layers.ToArray();
        var layout = await Task.Run(() => FbxTerrainMaterialLayout.Read(files, document.Descriptor, palette, token), token);
        token.ThrowIfCancellationRequested(); document.ApplySourceMaterialLayout(layout);
    }

    private async Task RestoreSourceLayoutAsync(CancellationToken token)
    {
        if (_level?.TerrainDocument is not { } document || _assets == null) return;
        if (!document.Descriptor.ImportedFromMesh) throw new InvalidOperationException("Уровень должен быть импортирован из FBX.");
        string[] files = SourceFiles();
        if (files.Length == 0)
            throw new FileNotFoundException("Исходный FBX не найден. Нажмите «Импортировать материалы из FBX» и выберите исходную землю.");
        await ApplySourceLayoutAsync(files, token);
        _status.Text = "Исходные участки материалов восстановлены. Назначьте текстуры; сохраните Terrain. Правку можно отменить.";
    }

    private async Task AssignTextureAsync(bool normal, CancellationToken token)
    {
        if (_assets == null || _level?.TerrainDocument is not { } document || _palette.SelectedItem is not TerrainLayer layer) return;
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage == null) return;
        var files = await storage.OpenFilePickerAsync(new()
        {
            Title = normal ? "Normal map материала" : "Текстура материала", AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Изображения") { Patterns = ["*.dds", "*.tga", "*.png", "*.jpg", "*.jpeg", "*.bmp"] }]
        });
        token.ThrowIfCancellationRequested();
        if (files.FirstOrDefault()?.TryGetLocalPath() is not { } file) return;
        string texture = await Task.Run(() => FbxTerrainMaterials.CopyTexture(_assets, file, token), token);
        token.ThrowIfCancellationRequested();
        float[] average = normal ? [] : await Task.Run(() => TerrainMaterialColor.Read(AssetPathResolver.Resolve(_assets.Root, texture)), token);
        token.ThrowIfCancellationRequested();
        if (document.Descriptor.ImportedFromMesh && !document.Descriptor.HasSourceMaterialLayout && !string.IsNullOrEmpty(layer.SourceMaterial))
        {
            await ImportPaletteAsync(token);
            if (!document.Descriptor.HasSourceMaterialLayout)
                throw new InvalidOperationException("Назначение отменено: сначала выберите исходный FBX для восстановления участков материала.");
        }
        token.ThrowIfCancellationRequested();
        layer = document.Layers[layer.Index];

        var updated = normal
            ? layer with
            {
                NormalTexture = texture
            }
            : layer with
            {
                DiffuseTexture = texture,
                NeedsTexture = false,
                AverageColor = average
            };

        document.SetLayer(updated with
        {
            TextureVariants = updated.GetTextureVariants()
        });

        _status.Text = "Текстура назначена всем участкам материала. Сохраните Terrain.";
    }
}

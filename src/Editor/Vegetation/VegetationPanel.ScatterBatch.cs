using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Vegetation;

namespace Editor.Vegetation;

public sealed partial class VegetationPanel
{
    private readonly ComboBox _batchMaterial = new();

    private readonly TextBox _batchSearch = new()
    {
        Watermark = "Поиск модели SpeedTree..."
    };

    private readonly StackPanel _batchModelList = new()
    {
        Spacing = 4
    };

    private readonly TextBlock _batchModelCount = Text("");

    private readonly TextBlock _batchProgressText = Text("");

    private readonly ProgressBar _batchProgress = new()
    {
        Minimum = 0,
        Maximum = 1,
        Value = 0,
        Height = 12
    };

    private readonly Button _batchGenerate = new()
    {
        Content = "Генерировать весь уровень",
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private readonly Button _batchCancel = new()
    {
        Content = "Отменить генерацию",
        IsEnabled = false,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private readonly HashSet<string> _batchCheckedModels = new(
        StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _batchCancellation;
    private Task _batchTask = Task.CompletedTask;
    private bool _batchBinding;

    private void BuildScatterBatchControls(StackPanel parent)
    {
        parent.Children.Add(Title("ВЫБОР МОДЕЛЕЙ SPEEDTREE"));

        ScatterField(
            parent,
            "Материал Terrain для выбранных моделей",
            _batchMaterial);

        parent.Children.Add(_batchSearch);
        parent.Children.Add(_batchModelCount);

        var selectionButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var selectAll = new Button
        {
            Content = "Выбрать найденные"
        };

        var clear = new Button
        {
            Content = "Снять найденные"
        };

        selectAll.Click += (_, _) =>
        {
            SelectFilteredModels(true);
        };

        clear.Click += (_, _) =>
        {
            SelectFilteredModels(false);
        };

        selectionButtons.Children.Add(selectAll);
        selectionButtons.Children.Add(clear);

        parent.Children.Add(selectionButtons);

        parent.Children.Add(new ScrollViewer
        {
            Content = _batchModelList,
            MaxHeight = 260
        });

        parent.Children.Add(Text(
            "Отметьте модели, которые должны появляться " +
            "на выбранном материале. Настройки плотности, " +
            "масштаба и Seed находятся ниже."));

        _batchSearch.TextChanged += (_, _) =>
        {
            RebuildBatchModelList();
        };

        _batchMaterial.SelectionChanged += (_, _) =>
        {
            if (!_batchBinding)
            {
                LoadBatchModelSelection();
            }
        };

        RebuildBatchModelList();
    }

    private void BuildScatterBatchActions(StackPanel parent)
    {
        parent.Children.Add(Title("ГЕНЕРАЦИЯ ВСЕГО УРОВНЯ"));

        parent.Children.Add(Text(
            "Генератор обработает все тайлы Terrain, " +
            "сохраняя вручную расставленные деревья."));

        parent.Children.Add(_batchGenerate);
        parent.Children.Add(_batchCancel);
        parent.Children.Add(_batchProgress);
        parent.Children.Add(_batchProgressText);

        _batchGenerate.Click += async (_, _) =>
        {
            if (!_batchTask.IsCompleted)
            {
                return;
            }

            _batchTask = GenerateScatterAllAsync();

            try
            {
                await _batchTask;
            }
            catch (Exception exception)
            {
                _status.Text = exception.Message;
            }
        };

        _batchCancel.Click += (_, _) =>
        {
            _batchCancellation?.Cancel();
        };
    }

    private void RefreshBatchMaterials()
    {
        _batchBinding = true;

        try
        {
            int? selectedIndex =
                (_batchMaterial.SelectedItem as TerrainLayer)?.Index;

            var materials = _level?.TerrainDocument?.Layers
                .Where(layer =>
                    !layer.IsArchived &&
                    layer.Settings.SurfaceType is not (
                        TerrainSurfaceType.Water or
                        TerrainSurfaceType.Rock or
                        TerrainSurfaceType.Asphalt or
                        TerrainSurfaceType.Glass))
                .ToArray() ?? [];

            _batchMaterial.ItemsSource = materials;

            _batchMaterial.SelectedItem = materials.FirstOrDefault(
                layer => layer.Index == selectedIndex) ??
                materials.FirstOrDefault();
        }
        finally
        {
            _batchBinding = false;
        }

        LoadBatchModelSelection();
    }

    private void LoadBatchModelSelection()
    {
        _batchCheckedModels.Clear();

        if (_level != null &&
            _batchMaterial.SelectedItem is TerrainLayer material)
        {
            foreach (var rule in _level.Vegetation.ScatterRules)
            {
                if (rule.MaterialIndex == material.Index &&
                    rule.Enabled)
                {
                    _batchCheckedModels.Add(rule.Model);
                }
            }
        }

        RebuildBatchModelList();
    }

    private IEnumerable<TreeCatalogEntry> FilteredBatchModels()
    {
        string search = (_batchSearch.Text ?? "").Trim();

        return _entries.Where(entry =>
            entry.Name.Contains(
                search,
                StringComparison.OrdinalIgnoreCase));
    }

    private void RebuildBatchModelList()
    {
        _batchModelList.Children.Clear();

        int visible = 0;

        foreach (var entry in FilteredBatchModels())
        {
            visible++;

            var checkbox = new CheckBox
            {
                Content = entry.Name,
                IsChecked = _batchCheckedModels.Contains(entry.File)
            };

            checkbox.IsCheckedChanged += (_, _) =>
            {
                if (checkbox.IsChecked == true)
                {
                    _batchCheckedModels.Add(entry.File);
                }
                else
                {
                    _batchCheckedModels.Remove(entry.File);
                }

                UpdateBatchModelCount();
            };

            _batchModelList.Children.Add(checkbox);
        }

        if (visible == 0)
        {
            _batchModelList.Children.Add(Text(
                "Модели по запросу не найдены."));
        }

        UpdateBatchModelCount();
    }

    private void SelectFilteredModels(bool selected)
    {
        foreach (var entry in FilteredBatchModels())
        {
            if (selected)
            {
                _batchCheckedModels.Add(entry.File);
            }
            else
            {
                _batchCheckedModels.Remove(entry.File);
            }
        }

        RebuildBatchModelList();
    }

    private void UpdateBatchModelCount()
    {
        _batchModelCount.Text =
            $"Всего моделей: {_entries.Count} · " +
            $"Отмечено: {_batchCheckedModels.Count}";
    }

    private List<VegetationScatterRule> PrepareBatchRules(
        LoadedLevel level,
        TerrainLayer material,
        IReadOnlyList<TreeCatalogEntry> selectedModels)
    {
        if (!int.TryParse(_scatterSeed.Text, out int seed))
        {
            throw new InvalidOperationException(
                "Seed должен быть целым числом.");
        }

        var kind = _scatterKind.SelectedItem is
            VegetationScatterKind selectedKind
                ? selectedKind
                : VegetationScatterKind.Tree;

        var template = new VegetationScatterRule
        {
            Kind = kind,
            MaterialIndex = material.Index,
            Seed = seed,

            DensityPer100SquareMeters = Read(_scatterDensity),

            MinimumScale = Read(_scatterScaleMin),
            MaximumScale = Read(_scatterScaleMax),

            MinimumMaterialWeight = Read(_scatterWeight),
            MaximumSlopeDegrees = Read(_scatterSlope),

            ObjectClearance = Read(_scatterObjectDistance),
            RoadClearance = Read(_scatterRoadDistance),
            MinimumSpacing = Read(_scatterSpacing),

            NearWaterOnly = _scatterNearWater.IsChecked == true,
            MaximumShoreDistance = Read(_scatterShoreDistance),

            Enabled = true
        };

        var rules = level.Vegetation.ScatterRules.ToList();

        // Отключаем старые правила этого материала.
        // Правила других материалов не затрагиваем.
        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i].MaterialIndex == material.Index)
            {
                rules[i] = rules[i] with { Enabled = false };
            }
        }

        foreach (var model in selectedModels.OrderBy(
            item => item.File,
            StringComparer.OrdinalIgnoreCase))
        {
            int index = rules.FindIndex(rule =>
                rule.MaterialIndex == material.Index &&
                string.Equals(
                    rule.Model,
                    model.File,
                    StringComparison.OrdinalIgnoreCase));

            var previous = index >= 0
                ? rules[index]
                : null;

            var rule = template with
            {
                Id = previous?.Id ?? Guid.NewGuid(),
                Name = previous?.Name ?? model.Name,
                Model = model.File,
                Enabled = true
            };

            VegetationScatterGenerator.ValidateRule(rule);

            if (index >= 0)
            {
                rules[index] = rule;
            }
            else
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    private async Task GenerateScatterAllAsync()
    {
        RequireEditable();

        if (_level?.TerrainDocument is not { } document ||
            _assets == null)
        {
            throw new InvalidOperationException(
                "Сначала откройте уровень.");
        }

        if (_batchMaterial.SelectedItem is not TerrainLayer material)
        {
            throw new InvalidOperationException(
                "Выберите материал Terrain.");
        }

        var selectedModels = _entries
            .Where(entry => _batchCheckedModels.Contains(entry.File))
            .ToArray();

        if (selectedModels.Length == 0)
        {
            throw new InvalidOperationException(
                "Отметьте хотя бы одну модель SpeedTree.");
        }

        var level = _level;
        var scene = level.Vegetation;

        var rules = PrepareBatchRules(
            level,
            material,
            selectedModels);

        var descriptor = document.Descriptor;

        int totalTiles = checked(
            descriptor.TilesX * descriptor.TilesZ);

        if (totalTiles == 0)
        {
            throw new InvalidOperationException(
                "У уровня отсутствуют тайлы Terrain.");
        }

        // Отдельная временная сцена. Реальные экземпляры
        // не изменяются до завершения всей генерации.
        var staged = new VegetationScene();

        staged.Instances.AddRange(scene.Instances);
        staged.ScatterRules.AddRange(rules);

        foreach (var asset in scene.Assets)
        {
            staged.Assets.Add(asset.Key, asset.Value);
        }

        var temporaryLevel = level with
        {
            Vegetation = staged
        };

        var cancellation = new CancellationTokenSource();

        _batchCancellation = cancellation;

        IsSaving = true;
        BusyChanged?.Invoke();
        RefreshMode();

        _batchGenerate.IsEnabled = false;
        _batchCancel.IsEnabled = true;
        _batchMaterial.IsEnabled = false;
        _batchSearch.IsEnabled = false;
        _batchModelList.IsEnabled = false;

        _batchProgress.Maximum = totalTiles;
        _batchProgress.Value = 0;

        int processed = 0;

        try
        {
            for (int tz = 0; tz < descriptor.TilesZ; tz++)
            {
                for (int tx = 0; tx < descriptor.TilesX; tx++)
                {
                    cancellation.Token.ThrowIfCancellationRequested();

                    int currentX = tx;
                    int currentZ = tz;

                    // Выполняем один тайл между кадрами UI.
                    // Все обращения к TerrainDocument остаются
                    // на потоке редактора.
                    await Dispatcher.UIThread.InvokeAsync(
                        () =>
                        {
                            cancellation.Token.ThrowIfCancellationRequested();

                            VegetationScatterGenerator.RegenerateTile(
                                temporaryLevel,
                                _assets!,
                                currentX,
                                currentZ,
                                cancellation.Token);
                        },
                        DispatcherPriority.Background);

                    processed++;

                    _batchProgress.Value = processed;

                    _batchProgressText.Text =
                        $"Тайлы: {processed}/{totalTiles} · " +
                        $"Экземпляров: {staged.Generated.Count}";
                }
            }

            cancellation.Token.ThrowIfCancellationRequested();

            if (!ReferenceEquals(_level, level))
            {
                throw new InvalidOperationException(
                    "Уровень изменился во время генерации.");
            }

            // Результат полностью готов: заменяем
            // только автоматически созданные экземпляры.
            foreach (var asset in staged.Assets)
            {
                scene.Assets.TryAdd(asset.Key, asset.Value);
            }

            scene.ScatterRules.Clear();
            scene.ScatterRules.AddRange(rules);

            scene.Generated.Clear();
            scene.Generated.AddRange(staged.Generated);

            scene.IsDirty = true;

            RefreshScatterControls();
            RefreshBatchMaterials();
            UpdateSummary();

            _status.Text =
                $"Генерация завершена. " +
                $"Тайлов: {processed}, " +
                $"экземпляров: {scene.Generated.Count}. " +
                "Сохраните растительность.";
        }
        catch (OperationCanceledException)
        {
            _status.Text =
                "Генерация отменена. " +
                "Предыдущие экземпляры сохранены.";
        }
        finally
        {
            _batchCancellation = null;
            cancellation.Dispose();

            _batchGenerate.IsEnabled = true;
            _batchCancel.IsEnabled = false;
            _batchMaterial.IsEnabled = true;
            _batchSearch.IsEnabled = true;
            _batchModelList.IsEnabled = true;

            IsSaving = false;
            BusyChanged?.Invoke();
            RefreshMode();
        }
    }
}

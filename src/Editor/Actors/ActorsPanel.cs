using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Actors;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;

namespace Editor.Actors
{
    public sealed partial class ActorsPanel : UserControl
    {
        private readonly StackPanel _browser = new()
        {
            Spacing = 8
        };

        private readonly StackPanel _properties = new()
        {
            Spacing = 8
        };

        private readonly ListBox _actorList = new()
        {
            MaxHeight = 420
        };

        private readonly ComboBox _actorKind = new()
        {
            ItemsSource = Enum.GetValues<ActorKind>(),
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        private readonly TextBlock _catalogStatus = new()
        {
            TextWrapping = TextWrapping.Wrap
        };

        private DxViewportHost? _viewport;
        private AssetPaths? _assets;
        private LoadedLevel? _level;
        private ActorItemCatalog? _catalog;

        private bool _refreshing;
        private bool _catalogLoading;

        public Control BrowserContent => _browser;

        private ActorDocument? Document => _level?.Actors;

        public bool HasPendingChanges =>
            Document?.HasPendingChanges == true;

        private LevelActor? SelectedActor =>
            (_actorList.SelectedItem as ListBoxItem)?.Tag as LevelActor;

        public ActorsPanel()
        {
            Content = _properties;

            _browser.Children.Add(new TextBlock
            {
                Text = "Тип нового актора"
            });

            _browser.Children.Add(_actorKind);

            _browser.Children.Add(new TextBlock
            {
                Text = "Ctrl + ЛКМ — добавить\nЛКМ — выбрать",
                TextWrapping = TextWrapping.Wrap
            });

            _browser.Children.Add(_actorList);

            _browser.Children.Add(CreateButton(
                "Обновить каталог предметов",
                async () => await LoadCatalogAsync(true)));

            _browser.Children.Add(_catalogStatus);

            _actorKind.SelectionChanged += (_, _) =>
            {
                if (_viewport != null &&
                    _actorKind.SelectedItem is ActorKind kind)
                {
                    _viewport.ActorPlacementKind = kind;
                }
            };

            _actorList.SelectionChanged += (_, _) =>
            {
                if (!_refreshing)
                {
                    UpdateSelection();
                }
            };
        }

        public void Attach(
            DxViewportHost viewport,
            AssetPaths assets)
        {
            _viewport = viewport;
            _assets = assets;

            viewport.ActorPicked += RefreshList;
            viewport.ActorChanged += id => RefreshList(id);
            viewport.ActorDeleteRequested += DeleteSelected;

            RefreshMode();
        }

        public void Bind(LoadedLevel level)
        {
            _level = level;

            RefreshList(null);
            RefreshMode();
        }

        public void SetActive(bool active)
        {
            IsVisible = active;
            RefreshMode();
        }

        public void RefreshMode()
        {
            if (_viewport == null)
            {
                return;
            }

            _viewport.ActorEditingEnabled =
                IsVisible &&
                IsEnabled &&
                _level != null &&
                !_viewport.IsPlaying &&
                !_viewport.IsStarting;

            if (_actorKind.SelectedItem is ActorKind kind)
            {
                _viewport.ActorPlacementKind = kind;
            }
        }

        public async Task LoadCatalogAsync(bool reload = false)
        {
            if (_assets == null ||
                _catalogLoading ||
                (!reload && _catalog != null))
            {
                return;
            }

            _catalogLoading = true;
            _catalogStatus.Text = "Загрузка каталога...";

            try
            {
                AssetPaths assets = _assets;

                _catalog = await Task.Run(
                    () => ActorItemCatalog.Read(assets));

                _catalogStatus.Text =
                    $"Предметов: {_catalog.Items.Count}";

                if (_catalog.Warnings.Count > 0)
                {
                    _catalogStatus.Text +=
                        $"\nОшибок каталога: {_catalog.Warnings.Count}";
                }

                ToolTip.SetTip(
                    _catalogStatus,
                    string.Join("\n", _catalog.Warnings));

                BuildProperties();
            }
            catch (Exception exception)
            {
                _catalogStatus.Text = exception.Message;
            }
            finally
            {
                _catalogLoading = false;
            }
        }

        public async Task SavePendingAsync()
        {
            if (_level == null || !HasPendingChanges)
            {
                return;
            }

            await ActorStorage.SaveAsync(
                _level.Directory,
                _level.Actors);
        }

        private void RefreshList(Guid? selectedId)
        {
            _refreshing = true;

            try
            {
                var rows = Document?.Actors
                    .Select(actor => new ListBoxItem
                    {
                        Content = FormatActor(actor),
                        Tag = actor
                    })
                    .ToArray() ?? [];

                _actorList.ItemsSource = rows;

                _actorList.SelectedItem = rows.FirstOrDefault(
                    row => row.Tag is LevelActor actor &&
                           actor.Id == selectedId);
            }
            finally
            {
                _refreshing = false;
            }

            UpdateSelection();
        }

        private void UpdateSelection()
        {
            _viewport?.SelectActor(SelectedActor?.Id);
            BuildProperties();
        }

        private static string FormatActor(LevelActor actor)
        {
            string prefix = actor.Enabled ? "" : "[OFF] ";

            return $"{prefix}{actor.Name} · {actor.Kind}";
        }

        private void Changed()
        {
            Document?.Touch();

            if (_actorList.SelectedItem is ListBoxItem row &&
                row.Tag is LevelActor actor)
            {
                row.Content = FormatActor(actor);
            }
        }

        public void DeleteSelected()
        {
            if (!IsVisible ||
                !IsEnabled ||
                _viewport?.ActorEditingEnabled != true ||
                SelectedActor is not { } actor ||
                Document == null)
            {
                return;
            }

            Document.Remove(actor.Id);
            RefreshList(null);
        }

        private void BuildProperties()
        {
            _properties.Children.Clear();

            LevelActor? actor = SelectedActor;

            if (actor == null)
            {
                _properties.Children.Add(new TextBlock
                {
                    Text = "Выберите или разместите актор.",
                    TextWrapping = TextWrapping.Wrap
                });

                return;
            }

            _properties.Children.Add(new TextBlock
            {
                Text = actor.Kind.ToString(),
                FontSize = 16,
                FontWeight = FontWeight.SemiBold
            });

            AddText(
                _properties,
                "Название",
                actor.Name,
                value =>
                {
                    actor.Name = value;
                    Changed();
                });

            var enabled = new CheckBox
            {
                Content = "Активен",
                IsChecked = actor.Enabled
            };

            enabled.IsCheckedChanged += (_, _) =>
            {
                actor.Enabled = enabled.IsChecked == true;
                Changed();
            };

            _properties.Children.Add(enabled);

            AddNumber(
                _properties, "Position X",
                (decimal)actor.X,
                -10000000, 10000000, 0.1m,
                value =>
                {
                    actor.X = (float)value;
                    Changed();
                });

            AddNumber(
                _properties, "Position Y",
                (decimal)actor.Y,
                -10000000, 10000000, 0.1m,
                value =>
                {
                    actor.Y = (float)value;
                    Changed();
                });

            AddNumber(
                _properties, "Position Z",
                (decimal)actor.Z,
                -10000000, 10000000, 0.1m,
                value =>
                {
                    actor.Z = (float)value;
                    Changed();
                });

            float degrees = actor.Yaw * 180f / MathF.PI;

            AddNumber(
                _properties, "Rotation Y",
                (decimal)Math.IEEERemainder(degrees, 360),
                -360, 360, 1,
                value =>
                {
                    actor.Yaw =
                        (float)value * MathF.PI / 180f;

                    Changed();
                });

            if (actor.Kind == ActorKind.NpcSpawn)
            {
                BuildNpcProperties(actor);
            }

            if (actor.Kind == ActorKind.MobSpawn)
            {
                AddText(
                    _properties,
                    "Template ID",
                    actor.TemplateId,
                    value =>
                    {
                        actor.TemplateId = value;
                        Changed();
                    });
            }

            if (actor.Kind == ActorKind.MobSpawn)
            {
                AddNumber(
                    _properties, "Радиус спавна, м",
                    (decimal)actor.Radius,
                    0.1m, 100, 0.5m,
                    value =>
                    {
                        actor.Radius = (float)value;
                        Changed();
                    });

                AddNumber(
                    _properties, "Количество мобов",
                    actor.Count,
                    1, 1000, 1,
                    value =>
                    {
                        actor.Count = (int)value;
                        Changed();
                    });
            }

            if (actor.Kind != ActorKind.PlayerSpawn)
            {
                AddNumber(
                    _properties, "Respawn, секунд",
                    actor.RespawnSeconds,
                    0, 604800, 1,
                    value =>
                    {
                        actor.RespawnSeconds = (int)value;
                        Changed();
                    });
            }

            if (actor.Kind == ActorKind.LootSpawn)
            {
                BuildLootProperties(actor);
            }

            _properties.Children.Add(CreateButton(
                "Переместить на сцене",
                () => _viewport?.BeginActorMove()));

            _properties.Children.Add(CreateButton(
                "Удалить актор",
                DeleteSelected));
        }

        private void BuildLootProperties(LevelActor actor)
        {
            _properties.Children.Add(new Separator());

            _properties.Children.Add(new TextBlock
            {
                Text = $"Loot Table ({actor.Loot.Count})",
                FontWeight = FontWeight.SemiBold
            });

            for (int index = 0; index < actor.Loot.Count; index++)
            {
                LootSpawnEntry entry = actor.Loot[index];

                var section = new StackPanel
                {
                    Spacing = 6
                };

                section.Children.Add(new TextBlock
                {
                    Text = $"Предмет {index + 1}",
                    FontWeight = FontWeight.SemiBold
                });

                BuildLootEntry(section, entry);

                section.Children.Add(CreateButton(
                    "Удалить предмет",
                    () =>
                    {
                        actor.Loot.Remove(entry);
                        Changed();
                        BuildProperties();
                    }));

                _properties.Children.Add(new Border
                {
                    BorderBrush = Brushes.DimGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8),
                    Child = section
                });
            }

            Button add = CreateButton(
                "+ Добавить предмет",
                () =>
                {
                    ActorItemDefinition? item =
                        _catalog?.Items.FirstOrDefault();

                    if (item == null)
                    {
                        return;
                    }

                    actor.Loot.Add(new LootSpawnEntry
                    {
                        TypeId = item.TypeId,
                        Category = item.Category
                    });

                    Changed();
                    BuildProperties();
                });

            add.IsEnabled = _catalog?.Items.Count > 0;

            _properties.Children.Add(add);
        }

        private void BuildLootEntry(
            StackPanel section,
            LootSpawnEntry entry)
        {
            string[] categories = (_catalog?.Items
                .Select(item => item.Category) ??
                Enumerable.Empty<string>())
                .Append(entry.Category)
                .Distinct()
                .Order()
                .ToArray();

            AddChoice(
                section,
                "Категория",
                categories,
                entry.Category,
                category =>
                {
                    ActorItemDefinition? item =
                        _catalog?.FindCategory(category)
                            .FirstOrDefault();

                    if (item == null)
                    {
                        return;
                    }

                    entry.Category = category;
                    entry.TypeId = item.TypeId;

                    Changed();
                    BuildProperties();
                });

            var items = (_catalog?.FindCategory(entry.Category) ?? [])
                .ToList();

            ActorItemDefinition? selected = items.FirstOrDefault(
                item => item.TypeId == entry.TypeId);

            if (selected == null)
            {
                selected = new ActorItemDefinition(
                    entry.TypeId,
                    0,
                    entry.Category,
                    "Отсутствует в каталоге");

                items.Insert(0, selected);
            }

            AddChoice(
                section,
                "Предмет",
                items,
                selected,
                item =>
                {
                    entry.TypeId = item.TypeId;
                    entry.Category = item.Category;
                    Changed();
                });

            AddNumber(
                section,
                "Шанс появления, %",
                entry.Chance,
                0, 100, 0.1m,
                value =>
                {
                    entry.Chance = value;
                    Changed();
                });

            AddChoice(
                section,
                "Редкость",
                Enum.GetValues<LootRarity>(),
                entry.Rarity,
                value =>
                {
                    entry.Rarity = value;
                    Changed();
                });

            var minimum = CreateNumber(
                entry.MinCount, 1, 10000, 1);

            var maximum = CreateNumber(
                entry.MaxCount, 1, 10000, 1);

            AddField(section, "Min Count", minimum);
            AddField(section, "Max Count", maximum);

            minimum.ValueChanged += (_, _) =>
            {
                if (minimum.Value is not decimal value)
                {
                    return;
                }

                entry.MinCount = (int)value;

                if (entry.MinCount > entry.MaxCount)
                {
                    maximum.Value = value;
                }

                Changed();
            };

            maximum.ValueChanged += (_, _) =>
            {
                if (maximum.Value is not decimal value)
                {
                    return;
                }

                entry.MaxCount = (int)value;

                if (entry.MaxCount < entry.MinCount)
                {
                    minimum.Value = value;
                }

                Changed();
            };
        }

        private static void AddField(
            StackPanel panel,
            string title,
            Control control)
        {
            panel.Children.Add(new TextBlock
            {
                Text = title
            });

            panel.Children.Add(control);
        }

        private static void AddText(
            StackPanel panel,
            string title,
            string value,
            Action<string> changed)
        {
            var control = new TextBox
            {
                Text = value
            };

            control.TextChanged += (_, _) =>
                changed(control.Text ?? "");

            AddField(panel, title, control);
        }

        private static NumericUpDown CreateNumber(
            decimal value,
            decimal minimum,
            decimal maximum,
            decimal increment)
        {
            return new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Increment = increment,
                Value = Math.Clamp(value, minimum, maximum),
                FormatString = "0.###",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private static void AddNumber(
            StackPanel panel,
            string title,
            decimal value,
            decimal minimum,
            decimal maximum,
            decimal increment,
            Action<decimal> changed)
        {
            var control = CreateNumber(
                value,
                minimum,
                maximum,
                increment);

            control.ValueChanged += (_, _) =>
            {
                if (control.Value is decimal number)
                {
                    changed(number);
                }
            };

            AddField(panel, title, control);
        }

        private static void AddChoice<T>(
            StackPanel panel,
            string title,
            IEnumerable<T> values,
            T selected,
            Action<T> changed)
        {
            var control = new ComboBox
            {
                ItemsSource = values.ToArray(),
                SelectedItem = selected,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            control.SelectionChanged += (_, _) =>
            {
                if (control.SelectedItem is T value)
                {
                    changed(value);
                }
            };

            AddField(panel, title, control);
        }

        private static Button CreateButton(
            string title,
            Action action)
        {
            var button = new Button
            {
                Content = title,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            button.Click += (_, _) => action();

            return button;
        }
    }
}

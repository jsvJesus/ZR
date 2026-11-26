using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ZoneReborn.World.Actors;

namespace Editor.Actors
{
    public sealed partial class ActorsPanel
    {
        private void BuildLootLocationProperties(LevelActor actor)
        {
            _properties.Children.Add(new Separator());

            AddChoice(
                _properties,
                "Category",
                ActorItemCatalog.Categories,
                actor.LootCategory,
                value =>
                {
                    actor.LootCategory = value;

                    Changed();
                });

            _properties.Children.Add(new TextBlock
            {
                Text = $"Locations ({actor.LootLocations.Count})",
                FontWeight = FontWeight.SemiBold
            });

            var placement = new CheckBox
            {
                Content = "Add Location [Ctrl + ЛКМ]",
                IsChecked =
                    _viewport?.LootLocationPlacementEnabled == true
            };

            placement.IsCheckedChanged += (_, _) =>
            {
                if (_viewport != null)
                {
                    _viewport.LootLocationPlacementEnabled =
                        placement.IsChecked == true;
                }
            };

            _properties.Children.Add(placement);

            _properties.Children.Add(CreateButton(
                "Select LootSpawn",
                () =>
                {
                    _viewport?.SelectLootLocation(null);

                    if (_viewport != null)
                    {
                        _viewport.LootLocationPlacementEnabled = false;
                    }

                    BuildProperties();
                }));

            for (int index = 0;
                 index < actor.LootLocations.Count;
                 index++)
            {
                LootSpawnLocation location =
                    actor.LootLocations[index];

                bool selected =
                    _viewport?.SelectedLootLocationId == location.Id;

                var row = new Grid
                {
                    ColumnDefinitions =
                        new ColumnDefinitions("*,64")
                };

                Button select = CreateButton(
                    $"Location {index + 1}",
                    () =>
                    {
                        _viewport?.SelectLootLocation(location.Id);

                        BuildProperties();
                    });

                if (selected)
                {
                    select.Background = new SolidColorBrush(
                        Color.FromRgb(120, 42, 42));
                }

                Button delete = CreateButton(
                    "DEL",
                    () => DeleteLootLocation(actor, location));

                select.Margin = new Thickness(0, 0, 4, 0);

                Grid.SetColumn(select, 0);
                Grid.SetColumn(delete, 1);

                row.Children.Add(select);
                row.Children.Add(delete);

                _properties.Children.Add(row);

                if (selected)
                {
                    BuildLootLocationTransform(actor, location);
                }
            }

            var preview = new CheckBox
            {
                Content = "Show Loot Preview",
                IsChecked =
                    _viewport?.LootPreviewEnabled == true
            };

            preview.IsCheckedChanged += (_, _) =>
            {
                if (_viewport != null)
                {
                    _viewport.LootPreviewEnabled =
                        preview.IsChecked == true;
                }
            };

            _properties.Children.Add(preview);
        }

        private void BuildLootLocationTransform(
            LevelActor actor,
            LootSpawnLocation location)
        {
            AddNumber(
                _properties,
                "Local X",
                (decimal)location.X,
                -20000000,
                20000000,
                0.05m,
                value =>
                {
                    location.X = (float)value;

                    Changed();
                });

            AddNumber(
                _properties,
                "Local Y",
                (decimal)location.Y,
                -20000000,
                20000000,
                0.05m,
                value =>
                {
                    location.Y = (float)value;

                    Changed();
                });

            AddNumber(
                _properties,
                "Local Z",
                (decimal)location.Z,
                -20000000,
                20000000,
                0.05m,
                value =>
                {
                    location.Z = (float)value;

                    Changed();
                });
        }

        private void DeleteLootLocation(
            LevelActor actor,
            LootSpawnLocation location)
        {
            if (_viewport?.ActorEditingEnabled != true ||
                _viewport.IsActorDragging)
            {
                return;
            }

            if (_viewport.SelectedLootLocationId == location.Id)
            {
                _viewport.SelectLootLocation(null);
            }

            actor.LootLocations.Remove(location);

            Changed();

            BuildProperties();
        }
    }
}

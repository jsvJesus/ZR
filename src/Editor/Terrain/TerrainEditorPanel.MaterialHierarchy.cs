using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ZoneReborn.World.Terrain.Models;

namespace Editor.Terrain;

public sealed partial class TerrainEditorPanel
{
    private readonly Button _materialMoveUp = new()
    {
        Content = "Выше"
    };

    private readonly Button _materialMoveDown = new()
    {
        Content = "Ниже"
    };

    private readonly CheckBox _materialLock = new()
    {
        Content = "Защитить материал от перерисовки"
    };

    private readonly TextBlock _materialPriorityInfo = new()
    {
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap
    };

    private bool _updatingMaterialHierarchy;

    private void BuildMaterialHierarchyControls()
    {
        var controls = new StackPanel
        {
            Spacing = 8
        };

        controls.Children.Add(new TextBlock
        {
            Text = "Приоритет сверху вниз",
            FontWeight = FontWeight.Bold
        });

        controls.Children.Add(new TextBlock
        {
            Text = "Материалы выше защищены от кистей " +
                   "материалов ниже. Блокировка защищает " +
                   "материал независимо от его приоритета.",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });

        controls.Children.Add(_materialPriorityInfo);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        buttons.Children.Add(_materialMoveUp);
        buttons.Children.Add(_materialMoveDown);

        controls.Children.Add(buttons);
        controls.Children.Add(_materialLock);

        var section = new Expander
        {
            Header = "Иерархия и защита материалов",
            IsExpanded = true,
            Content = controls
        };

        _materialControls.Children.Insert(4, section);

        _materialMoveUp.Click += (_, _) =>
        {
            MoveSelectedMaterial(-1);
        };

        _materialMoveDown.Click += (_, _) =>
        {
            MoveSelectedMaterial(1);
        };

        _materialLock.Checked += (_, _) =>
        {
            ChangeSelectedMaterialLock(true);
        };

        _materialLock.Unchecked += (_, _) =>
        {
            ChangeSelectedMaterialLock(false);
        };

        _palette.SelectionChanged += (_, _) =>
        {
            RefreshMaterialHierarchy();
        };

        RefreshMaterialHierarchy();
    }

    private void MoveSelectedMaterial(int direction)
    {
        if (!Active ||
            _level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        try
        {
            _viewport?.EndTerrainPaint();

            document.MoveMaterial(layer.Index, direction);

            RefreshPalette(layer.Index);
            RefreshMode();

            _status.Text =
                "Приоритет материала изменён. Сохраните Terrain.";
        }
        catch (Exception error)
        {
            _status.Text = error.Message;
        }
    }

    private void ChangeSelectedMaterialLock(bool locked)
    {
        if (_updatingMaterialHierarchy ||
            !Active ||
            _level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer ||
            layer.Index == 0)
        {
            return;
        }

        try
        {
            _viewport?.EndTerrainPaint();

            document.SetMaterialLocked(layer.Index, locked);

            RefreshPalette(layer.Index);
            RefreshMode();

            _status.Text = locked
                ? "Материал защищён от перерисовки."
                : "Блокировка материала снята.";
        }
        catch (Exception error)
        {
            _status.Text = error.Message;
            RefreshMaterialHierarchy();
        }
    }

    private void RefreshMaterialHierarchy()
    {
        _updatingMaterialHierarchy = true;

        try
        {
            if (_level?.TerrainDocument is not { } document ||
                _palette.SelectedItem is not TerrainLayer layer)
            {
                _materialMoveUp.IsEnabled = false;
                _materialMoveDown.IsEnabled = false;
                _materialLock.IsEnabled = false;
                _materialLock.IsChecked = false;

                _materialPriorityInfo.Text =
                    "Выберите материал.";

                return;
            }

            var visible = document.GetMaterialHierarchy()
                .Where(item => !item.IsArchived)
                .ToArray();

            int position = Array.FindIndex(
                visible,
                item => item.Index == layer.Index);

            bool editable = Active && layer.Index != 0;

            _materialMoveUp.IsEnabled =
                editable && position > 0;

            _materialMoveDown.IsEnabled =
                editable &&
                position >= 0 &&
                position < visible.Length - 2;

            _materialLock.IsEnabled = editable;

            _materialLock.IsChecked = layer.IsPaintLocked;

            _materialPriorityInfo.Text =
                $"Позиция: {position + 1} / {visible.Length}\n" +
                $"Индекс материала Escape: {layer.Index}\n" +
                (layer.IsPaintLocked
                    ? "Состояние: защищён"
                    : "Состояние: доступен для рисования");
        }
        finally
        {
            _updatingMaterialHierarchy = false;
        }
    }
}

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Editor;

public sealed partial class MainWindow
{
    private Task<float?> AskTerrainGridAsync(string name)
    {
        var input = new TextBox { Text = "1" };
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.OrangeRed };
        var root = new StackPanel { Spacing = 10, Margin = new Thickness(18) };
        root.Children.Add(new TextBlock { Text = $"Создание Terrain: {name}", FontWeight = FontWeight.Bold });
        root.Children.Add(new TextBlock
        {
            Text = "Шаг сетки, м: 0,25–4. Меньший шаг точнее сохраняет края отверстий и увеличивает объём данных. Импортируется только земля.",
            TextWrapping = TextWrapping.Wrap
        });
        root.Children.Add(input); root.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var accept = new Button { Content = "Создать Terrain" }; var cancel = new Button { Content = "Отмена" };
        buttons.Children.Add(accept); buttons.Children.Add(cancel); root.Children.Add(buttons);
        var dialog = new Window { Title = "Импорт земли из FBX", Width = 460, Height = 285, CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = root };
        accept.Click += (_, _) =>
        {
            if (float.TryParse((input.Text ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture,
                out float value) && float.IsFinite(value) && value >= 0.25f && value <= 4) dialog.Close(value);
            else error.Text = "Укажите шаг от 0,25 до 4 метров.";
        };
        cancel.Click += (_, _) => dialog.Close(null);
        return dialog.ShowDialog<float?>(this);
    }
}

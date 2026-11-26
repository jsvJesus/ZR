using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Client.Configuration;
using Client.Gameplay;
using Runtime.Viewport;

namespace Client
{
    public sealed class GameHost : Window
    {
        private readonly DxViewportHost _viewport = new()
        {
            ShowGrid = false,
            IsGameClient = true
        };

        private readonly LocalGameSession _session = new();

        private readonly CancellationTokenSource _lifetime = new();

        private bool _started;
        private bool _closing;
        private bool _failed;

        public GameHost()
        {
            Title = "Client";

            Width = 1600;
            Height = 900;

            MinWidth = 640;
            MinHeight = 480;

            Background = Brushes.Black;

            WindowStartupLocation =
                WindowStartupLocation.CenterScreen;

            Content = _viewport;

            _viewport.RendererError += OnRendererError;
            _viewport.PlayStateChanged += OnPlayStateChanged;

            Opened += OnOpened;
            Closed += OnClosed;
        }

        private async void OnOpened(
            object? sender,
            EventArgs e)
        {
            Opened -= OnOpened;

            CancellationToken token = _lifetime.Token;

            try
            {
                ClientSettings settings = ClientSettings.Load();

                Width = settings.WindowWidth;
                Height = settings.WindowHeight;

                Title = $"Client - {settings.PlayerName}";

                DateTime deadline = DateTime.UtcNow.AddSeconds(10);

                while (!_viewport.IsRendererReady)
                {
                    token.ThrowIfCancellationRequested();

                    if (DateTime.UtcNow >= deadline)
                    {
                        throw new InvalidOperationException(
                            "DirectX 11 не был инициализирован.");
                    }

                    await Task.Delay(16, token);
                }

                await _session.StartAsync(
                    _viewport,
                    settings,
                    token);

                token.ThrowIfCancellationRequested();

                _started = true;
            }
            catch (OperationCanceledException)
                when (token.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                ShowError(exception.Message);
            }
        }

        private void OnPlayStateChanged()
        {
            if (!_started ||
                _closing ||
                _failed ||
                _viewport.IsPlaying ||
                _viewport.IsStarting)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (!_closing && !_failed)
                {
                    Close();
                }
            });
        }

        private void OnRendererError(string message)
        {
            Dispatcher.UIThread.Post(
                () => ShowError(message));
        }

        private void ShowError(string message)
        {
            if (_closing || _failed)
            {
                return;
            }

            _failed = true;

            _lifetime.Cancel();

            _viewport.StopPlay();

            Title = "Client - Ошибка";

            var closeButton = new Button
            {
                Content = "Закрыть",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            closeButton.Click += (_, _) => Close();

            Content = new StackPanel
            {
                Spacing = 20,

                Margin = new Thickness(40),

                HorizontalAlignment =
                    HorizontalAlignment.Center,

                VerticalAlignment =
                    VerticalAlignment.Center,

                Children =
                {
                    new TextBlock
                    {
                        Text = "Ошибка запуска игры",
                        FontSize = 26,
                        Foreground = Brushes.White,
                        HorizontalAlignment =
                            HorizontalAlignment.Center
                    },

                    new TextBlock
                    {
                        Text = message,
                        FontSize = 16,
                        Foreground = Brushes.IndianRed,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 700
                    },

                    closeButton
                }
            };
        }

        private void OnClosed(
            object? sender,
            EventArgs e)
        {
            _closing = true;

            _lifetime.Cancel();

            _viewport.RendererError -= OnRendererError;
            _viewport.PlayStateChanged -= OnPlayStateChanged;

            _viewport.StopPlay();

            _lifetime.Dispose();
        }
    }
}
